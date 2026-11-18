using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>Регистрация, вход и „кой съм аз“.</summary>
public class AuthService(AppDbContext db, JwtTokenService tokens)
{
    private readonly PasswordHasher<User> _hasher = new();

    /// <summary>
    /// Ролята идва от контролера (от адреса), не от тялото на заявката.
    /// Admin не може да се регистрира — той идва от миграцията.
    /// </summary>
    public async Task<UserDto> RegisterAsync(RegisterDto dto, string role)
    {
        if (role != Roles.Client && role != Roles.Technician)
        {
            throw AppException.BadRequest("Регистрация е възможна само като клиент или техник.");
        }

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email))
        {
            throw AppException.Conflict("Този имейл вече е регистриран.");
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = email,
            Phone = dto.Phone.Trim(),
            City = dto.City.Trim(),
            Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim(),
            Role = role
        };
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync();   // UNIQUE индексът пази и при две едновременни регистрации (-> 409)

        return ToDto(user);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // Едно и също съобщение за грешен имейл и грешна парола,
        // за да не може да се разбере кои имейли съществуват.
        if (user is null
            || _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password) == PasswordVerificationResult.Failed)
        {
            throw AppException.Unauthorized("Грешен имейл или парола.");
        }

        if (user.IsBlocked)
        {
            throw AppException.Forbidden("Акаунтът е блокиран.");
        }

        var (token, expiresAt) = tokens.CreateToken(user);
        return new LoginResponseDto(token, expiresAt, ToDto(user));
    }

    public async Task<UserDto> GetMeAsync(int userId)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw AppException.NotFound("Потребителят не съществува.");

        if (user.IsBlocked)
        {
            throw AppException.Forbidden("Акаунтът е блокиран.");
        }

        return ToDto(user);
    }

    private static UserDto ToDto(User u) => new(u.UserId, u.FullName, u.Email, u.City, u.Role);
}
