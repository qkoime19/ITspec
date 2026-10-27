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
