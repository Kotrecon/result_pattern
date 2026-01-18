using System;
using System.Collections.Generic;
using System.Linq;

namespace YourNamespace.Common;

// Необобщённый Result — для операций без возвращаемого значения (аналог void).
// Пример: "сохранить файл", "отправить уведомление".
public class Result
{
    public bool IsSuccess { get; }
    public IReadOnlyList<IError> Errors { get; }

    // Конструктор internal — чтобы никто не создал неконсистентный объект (например, успех с ошибками).
    protected internal Result(bool isSuccess, IEnumerable<IError> errors)
    {
        if (!isSuccess)
        {
            if (errors == null || !errors.Any())
                throw new InvalidOperationException(ResultErrorMessages.FailureRequiresErrors);
        }

        IsSuccess = isSuccess;
        Errors = (errors?.ToList() ?? new List<IError>()).AsReadOnly();
    }

    // Фабрики — единственный способ создания экземпляра.
    // Это обеспечивает согласованность: успех → Errors = пусто, ошибка → IsSuccess = false.
    public static Result Success() => new(true, Array.Empty<IError>());

    public static Result Failure(IError error) => new(false, new[] { error });

    public static Result Failure(IEnumerable<IError> errors) => new(false, errors);

    // OnSuccess / OnFailure — для императивного стиля (часто в консольных приложениях или контроллерах).
    // Не возвращают Result — значит, их нельзя чейнить. Это "конечные" операции.
    public void OnSuccess(Action action)
    {
        if (IsSuccess)
            action();
    }

    public void OnFailure(Action<IReadOnlyList<IError>> action)
    {
        if (!IsSuccess)
            action(Errors);
    }

    // Преобразование в обобщённый Result — нужно, чтобы в цепочке после void-операции продолжить работу.
    // Пример: SaveFile().ToResult(user).Map(...)
    public Result<T> ToResult<T>(T valueIfSuccess) =>
        IsSuccess ? Result<T>.Success(valueIfSuccess) : Result<T>.Failure(Errors);
}

// Обобщённый Result<T> — основной тип для большинства операций.
// Он либо содержит значение (успех), либо ошибки (неуспех). Никогда и то, и другое.
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public IReadOnlyList<IError> Errors { get; }

    protected internal Result(bool isSuccess, T value, IEnumerable<IError> errors)
    {
        if (!isSuccess)
        {
            if (errors == null || !errors.Any())
                throw new InvalidOperationException(ResultErrorMessages.FailureRequiresErrors);
        }

        IsSuccess = isSuccess;
        Value = value;
        Errors = (errors?.ToList() ?? new List<IError>()).AsReadOnly();
    }

    public static Result<T> Success(T value) => new(true, value, Array.Empty<IError>());

    public static Result<T> Failure(IError error) => new(false, default!, new[] { error });

    public static Result<T> Failure(IEnumerable<IError> errors) => new(false, default!, errors);

    public void OnSuccess(Action<T> action)
    {
        if (IsSuccess)
            action(Value);
    }

    public void OnFailure(Action<IReadOnlyList<IError>> action)
    {
        if (!IsSuccess)
            action(Errors);
    }

    // Map — "преобразовать значение, если успех".
    // Аналог LINQ Select: (Result<T> -> Result<U>).
    // Если ошибка — просто пропускаем преобразование и передаём ошибку дальше.
    public Result<TResult> Map<TResult>(Func<T, TResult> func)
    {
        return IsSuccess ? Result<TResult>.Success(func(Value)) : Result<TResult>.Failure(Errors);
    }

    // Bind — "вызвать следующую операцию, возвращающую Result, если успех".
    // Это основа для цепочек без вложенных if.
    // Если текущий результат — ошибка, то func не вызывается вообще.
    public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> func)
    {
        return IsSuccess ? func(Value) : Result<TResult>.Failure(Errors);
    }

    // Преобразование в необобщённый Result: полезно, когда значение больше не нужно в цепочке.
    public Result ToResult() => IsSuccess ? Result.Success() : Result.Failure(Errors);

    internal static class ResultErrorMessages
    {
        // Можно менять в одном месте или через условную компиляцию
        public static string FailureRequiresErrors =>
#if DEBUG
            "Неуспешный Result должен содержать хотя бы одну ошибку (IError). Проверьте вызовы Result.Failure().";
#else
            "A failure Result must contain at least one IError. This indicates a bug in the calling code.";
#endif
    }
}

internal static class ResultErrorMessages
{
    // Можно менять в одном месте или через условную компиляцию
    public static string FailureRequiresErrors =>
#if DEBUG
        "Неуспешный Result должен содержать хотя бы одну ошибку (IError). Проверьте вызовы Result.Failure().";
#else
        "A failure Result must contain at least one IError. This indicates a bug in the calling code.";
#endif
}
