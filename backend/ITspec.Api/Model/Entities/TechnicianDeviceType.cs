namespace ITspec.Api.Model.Entities;

/// <summary>
/// Междинна таблица за връзката N:M между техници и видове устройства.
/// Един техник работи с много видове, с един вид работят много техници.
/// Първичният ключ е съставен: (TechnicianId, DeviceTypeId).
/// </summary>
public class TechnicianDeviceType
{
    public int TechnicianId { get; set; }
    public User Technician { get; set; } = null!;

    public int DeviceTypeId { get; set; }
    public DeviceType DeviceType { get; set; } = null!;
}
