using ITspec.Api.View.DTOs;

namespace ITspec.Api.Model.Services;

/// <summary>
/// Стъпка 6б: каталогът (видове устройства) е списък в паметта — още няма база.
/// Сервизът е моделът (M): не знае за HTTP. Ако записът липсва, връща null.
/// </summary>
public class DeviceTypeService
{
    private readonly List<DeviceTypeDto> _items =
    [
        new(1, "Лаптоп", true, 0),
        new(2, "Смартфон", true, 0),
        new(3, "Принтер", true, 0)
    ];

    private readonly Lock _lock = new();
    private int _nextId = 4;

    public List<DeviceTypeDto> GetAll()
    {
        lock (_lock)
        {
            return _items.Where(d => d.IsActive).OrderBy(d => d.Name).ToList();
        }
    }

    public DeviceTypeDto? GetById(int id)
    {
        lock (_lock)
        {
            return _items.SingleOrDefault(d => d.DeviceTypeId == id && d.IsActive);
        }
    }

    public DeviceTypeDto Create(SaveDeviceTypeDto dto)
    {
        lock (_lock)
        {
            // Id го дава сървърът, не клиентът.
            var item = new DeviceTypeDto(_nextId++, dto.Name.Trim(), dto.IsActive, 0);
            _items.Add(item);
            return item;
        }
    }
}
