namespace ITspec.Api.Model.Entities;

/// <summary>
/// Ред от изгледа vw_OpenSupportRequests: „кои заявки чакат оферти?“.
/// Изгледът няма ключ (HasNoKey) и не съдържа адреса и телефона на клиента.
/// </summary>
public class OpenSupportRequestView
{
    public int SupportRequestId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DeviceModel { get; set; }
    public string City { get; set; } = string.Empty;
    public DateOnly? PreferredDate { get; set; }
    public int DeviceTypeId { get; set; }
    public string DeviceTypeName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int OffersCount { get; set; }
}
