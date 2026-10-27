using System.ComponentModel.DataAnnotations;
using ITspec.Api.Model.Entities;

namespace ITspec.Api.View.DTOs;

public class CreateOfferDto
{
    [Range(typeof(decimal), "0.01", "99999999", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true,
        ErrorMessage = "Цената трябва да е положителна.")]
    public decimal Price { get; set; }

    [StringLength(1000, ErrorMessage = "Съобщението е до 1000 символа.")]
    public string? Message { get; set; }

    public DateOnly? ProposedDate { get; set; }
}

public record OfferDto(
    int OfferId,
    int SupportRequestId,
    int TechnicianId,
    string TechnicianName,
    decimal? TechnicianRating,
    decimal Price,
    string? Message,
    DateOnly? ProposedDate,
    OfferStatus Status,
    DateTime CreatedAt);

/// <summary>Оферта в списъка „Моите оферти“ на техника.</summary>
public record MyOfferDto(
    int OfferId,
    int SupportRequestId,
    string RequestTitle,
    RequestStatus RequestStatus,
    decimal Price,
    DateOnly? ProposedDate,
    OfferStatus Status,
    DateTime CreatedAt);

public record AcceptOfferResultDto(int OfferId, int SupportRequestId, RequestStatus RequestStatus);
