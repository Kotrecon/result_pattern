// Program.cs
// Требует: using YourNamespace.Common; ← замените на ваше пространство имён

using YourNamespace.Common; // ← УКАЖИ СВОЁ ПРОСТРАНСТВО ИМЁН

namespace YourNamespace;

// Простая "сущность"
record User(int Id, string Email, bool IsActive);

class Program
{
    static void Main()
    {
        Console.WriteLine("🧪 Тестирование фреймворка Result<T>\n");

        // === Сценарий 1: Успешное создание пользователя ===
        TestCreateUser("user@example.com", "Иван");

        // === Сценарий 2: Ошибка валидации (пустой email) ===
        TestCreateUser("", "Анна");

        // === Сценарий 3: Цепочка операций с Bind ===
        SimulateGetUserById(1); // Успешно
        SimulateGetUserById(999); // NotFound

        // === Сценарий 4: Конфликт (дубликат email) ===
        TestEmailConflict("existing@example.com", "existing@example.com");
        TestEmailConflict("new@example.com", "existing@example.com");

        Console.WriteLine("\n✅ Все тесты выполнены!");
    }

    static void TestCreateUser(string email, string name)
    {
        var result = CreateUser(email, name);

        result.OnSuccess(user =>
        {
            Console.WriteLine($"✅ Пользователь создан: {user.Id}, {user.Email}");
        });

        result.OnFailure(errors =>
        {
            Console.WriteLine("❌ Ошибки создания:");
            foreach (var err in errors)
            {
                Console.WriteLine($"  [{err.Code}] {err.Message}");
                if (err is ValidationError ve)
                {
                    foreach (var detail in ve.Details)
                        Console.WriteLine($"    • {detail}");
                }
            }
        });
        Console.WriteLine();
    }

    static void SimulateGetUserById(int id)
    {
        // Имитируем "сервис"
        var getUserResult = GetUserById(id);

        var finalResult = getUserResult.Bind(user =>
            user.IsActive ? Result<User>.Success(user) : Result<User>.Failure(new ForbiddenError())
        );

        finalResult.OnSuccess(u =>
            Console.WriteLine($"✅ Получен активный пользователь: {u.Email}")
        );

        finalResult.OnFailure(errors =>
        {
            var err = errors[0];
            Console.WriteLine($"❌ {err.Code}: {err.Message} (HTTP {err.StatusCode})");
        });
        Console.WriteLine();
    }

    static void TestEmailConflict(string newEmail, string existingEmail)
    {
        var result = RegisterUser(newEmail, existingEmail);
        result.OnSuccess(_ => Console.WriteLine($"✅ Пользователь {newEmail} зарегистрирован"));
        result.OnFailure(errors =>
        {
            var err = errors[0];
            Console.WriteLine($"⚠️  {err.Code} ({err.StatusCode}): {err.Message}");
        });
        Console.WriteLine();
    }

    // === Фейковые "сервисы" ===

    static Result<User> CreateUser(string email, string name)
    {
        var errors = new List<IError>();

        if (string.IsNullOrWhiteSpace(email))
            errors.Add(
                new ValidationError(
                    "Email не может быть пустым",
                    new[] { "Поле Email обязательно для заполнения" }
                )
            );

        if (errors.Count > 0)
            return Result<User>.Failure(errors);

        // Успешно
        return Result<User>.Success(
            new User(Id: 123, Email: email.Trim().ToLower(), IsActive: true)
        );
    }

    static Result<User> GetUserById(int id)
    {
        // Имитируем БД: только ID 1 существует
        if (id == 1)
            return Result<User>.Success(new User(1, "admin@example.com", IsActive: true));
        else
            return Result<User>.Failure(new NotFoundError("User", id));
    }

    static Result<User> RegisterUser(string newEmail, string existingEmail)
    {
        if (newEmail == existingEmail)
            return Result<User>.Failure(
                new ConflictError("Пользователь с таким email уже существует")
            );
        return Result<User>.Success(new User(999, newEmail, true));
    }
}
