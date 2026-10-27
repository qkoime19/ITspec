namespace ITspec.Api.Model.Entities;

/// <summary>
/// Вид устройство (Лаптоп, Смартфон, Принтер, ...) — категориите на темата.
/// Не се трият, а се деактивират с IsActive = false.
/// </summary>
public class DeviceType
{
    public int DeviceTypeId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<TechnicianDeviceType> TechnicianDeviceTypes { get; set; } = new List<TechnicianDeviceType>();

    public ICollection<SupportRequest> SupportRequests { get; set; } = new List<SupportRequest>();
}
