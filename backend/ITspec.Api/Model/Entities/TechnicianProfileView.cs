namespace ITspec.Api.Model.Entities;

/// <summary>
/// Ред от изгледа vw_TechnicianProfiles: „какъв е този техник?“.
/// Без телефон и адрес, без блокираните техници.
/// </summary>
public class TechnicianProfileView
{
    public int TechnicianId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? DeviceTypes { get; set; }
    public decimal? AvgRating { get; set; }
    public int ReviewsCount { get; set; }
    public int CompletedCount { get; set; }
}
