using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Infrastructure.Errors;

// Една обща обработка на грешките:
//   AppException                      -> неговия статус код
//   THROW 50007 (оценка извън 1-5)    -> 400
//   THROW 50001-50099 от процедура    -> 409 със съобщението от процедурата
//   нарушен UNIQUE индекс (2601/2627) -> 409
//   нарушен CHECK / FOREIGN KEY (547) -> 400
//   всичко друго                      -> 500 без технически детайли
public class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            AppException app => (app.StatusCode, app.Message),
            SqlException sql when sql.Number == 50007 => (StatusCodes.Status400BadRequest, sql.Message),
            SqlException sql when sql.Number is >= 50001 and <= 50099 => (StatusCodes.Status409Conflict, sql.Message),
            DbUpdateException { InnerException: SqlException sql } when sql.Number is 2601 or 2627
                => (StatusCodes.Status409Conflict, "Такъв запис вече съществува."),
            DbUpdateException { InnerException: SqlException sql } when sql.Number == 547
                => (StatusCodes.Status400BadRequest, "Данните нарушават ограничение в базата."),
            _ => (StatusCodes.Status500InternalServerError, "Възникна неочаквана грешка.")
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Необработена грешка");
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }
}
