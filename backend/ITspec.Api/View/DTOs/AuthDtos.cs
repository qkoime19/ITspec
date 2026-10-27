using System.ComponentModel.DataAnnotations;

namespace ITspec.Api.View.DTOs;

/// <summary>
/// Входни данни за регистрация. Няма поле Role: ролята се определя от адреса
/// (POST auth/register/client или auth/register/technician). Ако клиентът
/// изпрати "role" в тялото, то просто се игнорира.
/// </summary>
public class RegisterDto
{
    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 100 символа.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Имейлът е задължителен.")]
    [EmailAddress(ErrorMessage = "Невалиден имейл.")]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Паролата е задължителна.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да е поне 6 символа.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефонът е задължителен.")]
    [RegularExpression(@"^\+?[0-9 ]{6,20}$", ErrorMessage = "Телефонът може да съдържа само цифри, интервали и + в началото.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Градът е задължителен.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Градът трябва да е между 2 и 60 символа.")]
    public string City { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Адресът е до 200 символа.")]
    public string? Address { get; set; }
}

public class LoginDto
{
    [Required(ErrorMessage = "Имейлът е задължителен.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Паролата е задължителна.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Изходни данни за потребител: никога PasswordHash.</summary>
public record UserDto(int UserId, string FullName, string Email, string City, string Role);

public record LoginResponseDto(string Token, DateTime ExpiresAt, UserDto User);
