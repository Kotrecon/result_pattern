using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace YourNamespace.Common;

// Ошибка валидации: используется, когда входные данные не соответствуют правилам.
// Хранит как общее сообщение, так и список конкретных нарушений (например, "поле X — обязательно").
public sealed class ValidationError : IError
{
    public string Message { get; }
    public string Code => "ValidationFailed";

    // 422 Unprocessable Entity — стандартный код для ошибок валидации (RFC 4918),
    // когда синтаксис запроса корректен, но семантика нарушена.
    public int StatusCode => 422; // 422 Unprocessable Entity — стандарт для валидации

    // Список деталей — чтобы клиент (или лог) знал, какие именно правила нарушены.
    // Используем IReadOnlyList для неизменяемости: нельзя случайно модифицировать после создания.
    public IReadOnlyList<string> Details { get; }

    public ValidationError(string message, IEnumerable<string> details)
    {
        Message = message;
        // Защищаемся от null и делаем коллекцию неизменяемой
        Details =
            details?.ToList().AsReadOnly() ?? new ReadOnlyCollection<string>(new List<string>());
    }
}

// Ошибка "не найдено": типична для операций поиска по ID.
// Содержит имя сущности и ID — это помогает в диагностике и локализации.
public sealed class NotFoundError : IError
{
    public string Message { get; }
    public string Code => "NotFound";
    public int StatusCode => 404;

    public string EntityName { get; }

    public NotFoundError(string entityName, object id)
    {
        EntityName = entityName;
        Message = $"{entityName} с ID '{id}' не найден.";
    }
}

// Ошибка доступа: клиент пытается сделать то, на что у него нет прав.
// HTTP-аналог — 403 Forbidden.
public sealed class ForbiddenError : IError
{
    public string Message { get; } = "Доступ запрещён.";
    public string Code => "Forbidden";
    public int StatusCode => 403;
}

// Общая ошибка бизнес-правила: например, "нельзя отменить оплаченный заказ".
// Используется, когда нарушена логика предметной области.
public sealed class BusinessRuleError : IError
{
    public string Message { get; }
    public string Code => "BusinessRuleViolation";
    public int StatusCode => 400; // или 422 — зависит от контекста

    public BusinessRuleError(string message) => Message = message;
}

// Ошибка конфликта: попытка создать/изменить ресурс, который уже существует
// или находится в состоянии, конфликтующем с запросом.
// Пример: регистрация пользователя с уже занятым email.
public sealed class ConflictError : IError
{
    public string Message { get; }
    public string Code => "Conflict";
    public int StatusCode => 409;

    public ConflictError(string message) => Message = message;
}
