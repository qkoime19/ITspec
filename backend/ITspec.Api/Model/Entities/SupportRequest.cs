namespace ITspec.Api.Model.Entities;

/// <summary>
/// Заявка за помощ с компютър или телефон, публикувана от клиент.
/// </summary>
public class SupportRequest
{
    public int SupportRequestId { get; set; }

    public int ClientId { get; set; }
    public User Client { get; set; } = null!;

    public int DeviceTypeId { get; set; }
    public DeviceType DeviceType { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    /// <summary>Описание на проблема.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Марка и модел, напр. „Lenovo IdeaPad 5“. Може да липсва.</summary>
    public string? DeviceModel { get; set; }

    public string City { get; set; } = string.Empty;

    /// <summary>Само дата, без час. Може да липсва.</summary>
    public DateOnly? PreferredDate { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.Open;

    public DateTime CreatedAt { get; set; }

    // --- Навигации ---

    /// <summary>Офертите на техниците за тази заявка (връзка 1:N).</summary>
    public ICollection<Offer> Offers { get; set; } = new List<Offer>();

    /// <summary>Отзивът след завършване (връзка 1:0..1).</summary>
    public Review? Review { get; set; }
}
