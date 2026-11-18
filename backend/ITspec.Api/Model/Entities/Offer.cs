namespace ITspec.Api.Model.Entities;

/// <summary>
/// Оферта на техник към заявка. Приетата оферта е и „работата“:
/// тя казва кой, кога и за колко ще реши проблема.
/// </summary>
public class Offer
{
    public int OfferId { get; set; }

    public int SupportRequestId { get; set; }
    public SupportRequest SupportRequest { get; set; } = null!;

    public int TechnicianId { get; set; }
    public User Technician { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Message { get; set; }

    public DateOnly? ProposedDate { get; set; }

    public OfferStatus Status { get; set; } = OfferStatus.Pending;

    public DateTime CreatedAt { get; set; }
}
