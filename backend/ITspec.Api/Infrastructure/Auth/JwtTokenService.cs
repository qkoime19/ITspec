using System.Security.Claims;
using System.Text;
using ITspec.Api.Model.Entities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ITspec.Api.Infrastructure.Auth;

// Издава JWT с Id, име, имейл и роля. Ключът е в User Secrets (Jwt:Key), не в appsettings.json.
public class JwtTokenService(IConfiguration configuration)
{
    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var jwt = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.GetValue("ExpiresMinutes", 120));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt["Issuer"],
            Audience = jwt["Audience"],
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(AuthClaims.UserId, user.UserId.ToString()),
                new Claim(AuthClaims.Name, user.FullName),
                new Claim(AuthClaims.Email, user.Email),
                new Claim(AuthClaims.Role, user.Role)
            })
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return (token, expiresAt);
    }
}
