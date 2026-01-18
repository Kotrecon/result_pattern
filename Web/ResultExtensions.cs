using Microsoft.AspNetCore.Mvc.Core;
using YourNamespace.Common;

namespace YourNamespace.Web;

// Расширения только для ASP.NET Core.
// Ядро (Result) не знает о HTTP — это важно для чистоты архитектуры.
public static class ResultExtensions
{
    // Преобразует Result<T> в IActionResult.
    // Использует первую ошибку для определения HTTP-статуса.
    // Это упрощение: в реальных системах можно обрабатывать все ошибки,
    // но обычно клиенту важна первая (или самая критичная).
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        var firstError = result.Errors[0]; // ← безопасно благодаря защите в конструкторе

        // Используем стандартный ProblemDetails (RFC 7807) — это рекомендованный способ в ASP.NET Core.
        var problem = new ProblemDetails
        {
            Status = firstError.StatusCode,
            Title = firstError.Code, // Код ошибки → заголовок
            Detail = firstError.Message, // Сообщение → детали
        };

        // Для ValidationError добавляем детали валидации в расширения.
        // ASP.NET Core автоматически сериализует Extensions в JSON.
        if (firstError is ValidationError ve)
        {
            problem.Extensions["errors"] = ve.Details;
        }

        // ObjectResult с явным StatusCode — гарантирует правильный HTTP-код.
        return new ObjectResult(problem) { StatusCode = firstError.StatusCode };
    }

    // Перегрузка для необобщённого Result.
    public static IActionResult ToActionResult(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        var firstError = result.Errors[0]; // ← безопасно благодаря защите в конструкторе

        var problem = new ProblemDetails
        {
            Status = firstError.StatusCode,
            Title = firstError.Code,
            Detail = firstError.Message,
        };

        return new ObjectResult(problem) { StatusCode = firstError.StatusCode };
    }
}
