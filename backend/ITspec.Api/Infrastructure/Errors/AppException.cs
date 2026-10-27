namespace ITspec.Api.Infrastructure.Errors;

// Грешка от бизнес логиката с HTTP статус код. ApiExceptionHandler я превръща в ProblemDetails.
public class AppException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static AppException NotFound(string message) => new(StatusCodes.Status404NotFound, message);
    public static AppException Forbidden(string message) => new(StatusCodes.Status403Forbidden, message);
    public static AppException Conflict(string message) => new(StatusCodes.Status409Conflict, message);
    public static AppException BadRequest(string message) => new(StatusCodes.Status400BadRequest, message);
    public static AppException Unauthorized(string message) => new(StatusCodes.Status401Unauthorized, message);
}
