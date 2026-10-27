using System.ComponentModel.DataAnnotations;
using ITspec.Api.Model.Entities;

namespace ITspec.Api.View.DTOs;

public class CreateRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Изберете вид устройство.")]
    public int DeviceTypeId { get; set; }

    [Required(ErrorMessage = "Заглавието е задължително.")]
    [StringLength(120, MinimumLength = 5, ErrorMessage = "Заглавието трябва да е между 5 и 120 символа.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Описанието е задължително.")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Опишете проблема с поне 10 символа.")]
    public string Description { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Моделът е до 100 символа.")]
    public string? DeviceModel { get; set; }

    /// <summary>Ако липсва, се взима градът на клиента.</summary>
    [StringLength(60, ErrorMessage = "Градът е до 60 символа.")]
    public string? City { get; set; }

    public DateOnly? PreferredDate { get; set; }
}

/// <summary>Ред в списъка със заявки. Без контактни данни.</summary>
public record RequestListItemDto(
    int SupportRequestId,
    string Title,
    string DeviceTypeName,
    string? DeviceModel,
    string City,
    DateOnly? PreferredDate,
    RequestStatus Status,
    DateTime CreatedAt,
    int OffersCount,
    string ClientName);

/// <summary>
/// Детайли на заявка. ClientPhone и ClientAddress са попълнени САМО за техника
/// с приетата оферта — за всички останали са null (скритите данни на темата).
/// </summary>
public record RequestDetailsDto(
    int SupportRequestId,
    string Title,
    string Description,
    string? DeviceModel,
    int DeviceTypeId,
    string DeviceTypeName,
    string City,
    DateOnly? PreferredDate,
    RequestStatus Status,
    DateTime CreatedAt,
    int ClientId,
    string ClientName,
    string? ClientPhone,
    string? ClientAddress,
    AcceptedOfferDto? AcceptedOffer,
    OfferDto? MyOffer,
    ReviewDto? Review);

public record AcceptedOfferDto(int OfferId, int TechnicianId, string TechnicianName, decimal Price, DateOnly? ProposedDate);

public class CreateReviewDto
{
    [Range(1, 5, ErrorMessage = "Оценката е от 1 до 5.")]
    public byte Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Коментарът е до 1000 символа.")]
    public string? Comment { get; set; }
}

public record ReviewDto(int ReviewId, byte Rating, string? Comment, DateTime CreatedAt);
