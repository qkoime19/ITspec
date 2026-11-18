using System.Security.Claims;

namespace ITspec.Api.Infrastructure.Auth;

// Имената на claim-овете в JWT токена. В токена има само Id, име, имейл и роля — без парола, телефон и адрес.
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";

    // Id на текущия потребител — винаги от токена, никога от тялото или адреса на заявката.
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirst(UserId)?.Value;
        return int.TryParse(value, out var id)
            ? id
            : throw new InvalidOperationException("Токенът няма валиден sub claim.");
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        return user.FindFirst(Role)?.Value ?? "";
    }
}
