using System.ComponentModel.DataAnnotations;

namespace ITspec.Api.View.DTOs;

/// <summary>Публичен профил на техник (от vw_TechnicianProfiles). Без телефон и адрес.</summary>
public record TechnicianProfileDto(
    int TechnicianId,
    string FullName,
    string City,
    string? DeviceTypes,
    decimal? AvgRating,
    int ReviewsCount,
    int CompletedCount);

public record TechnicianReviewDto(byte Rating, string? Comment, string RequestTitle, DateTime CreatedAt);

public record TechnicianDetailsDto(TechnicianProfileDto Profile, List<TechnicianReviewDto> Reviews);

public class SetDeviceTypesDto
{
    public List<int> DeviceTypeIds { get; set; } = new();
}

/// <summary>Собственият профил на техника (вижда го само той). Без парола и без роля.</summary>
public record MyProfileDto(string FullName, string Email, string? Phone, string City, decimal? AvgRating, int ReviewsCount);

/// <summary>Какво техникът може да промени в профила си. Няма имейл, роля, рейтинг и Id.</summary>
public class UpdateProfileDto
{
    [Required(ErrorMessage = "Името е задължително.")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Името трябва да е между 3 и 100 символа.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Телефонът е задължителен.")]
    [RegularExpression(@"^\+?[0-9 ]{6,20}$", ErrorMessage = "Невалиден телефон.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Градът е задължителен.")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Градът трябва да е между 2 и 60 символа.")]
    public string City { get; set; } = string.Empty;
}
