using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>Видовете устройства (категориите). Променя ги само Admin.</summary>
public class DeviceTypeService(AppDbContext db)
{
    public async Task<List<DeviceTypeDto>> GetAllAsync(bool includeInactive)
    {
        return await db.DeviceTypes
            .Where(d => includeInactive || d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DeviceTypeDto(d.DeviceTypeId, d.Name, d.IsActive, d.SupportRequests.Count))
            .ToListAsync();
    }

    /// <summary>Един активен вид. Неактивен (изтрит) или несъществуващ -> 404.</summary>
    public async Task<DeviceTypeDto> GetByIdAsync(int id)
    {
        return await db.DeviceTypes
            .Where(d => d.DeviceTypeId == id && d.IsActive)
            .Select(d => new DeviceTypeDto(d.DeviceTypeId, d.Name, d.IsActive, d.SupportRequests.Count))
            .SingleOrDefaultAsync()
            ?? throw AppException.NotFound("Видът устройство не съществува.");
    }

    public async Task<DeviceTypeDto> CreateAsync(SaveDeviceTypeDto dto)
    {
        var name = dto.Name.Trim();
        if (await db.DeviceTypes.AnyAsync(d => d.Name == name))
        {
            throw AppException.Conflict("Вече има вид устройство с това име.");
        }

        var type = new DeviceType { Name = name, IsActive = dto.IsActive };
        db.DeviceTypes.Add(type);
        await db.SaveChangesAsync();

        // Id е 0 до SaveChangesAsync: стойността идва от IDENTITY колоната в базата.
        return new DeviceTypeDto(type.DeviceTypeId, type.Name, type.IsActive, 0);
    }

    public async Task UpdateAsync(int id, SaveDeviceTypeDto dto)
    {
        var type = await db.DeviceTypes.FindAsync(id)
            ?? throw AppException.NotFound("Видът устройство не съществува.");

        var name = dto.Name.Trim();
        if (await db.DeviceTypes.AnyAsync(d => d.Name == name && d.DeviceTypeId != id))
        {
            throw AppException.Conflict("Вече има вид устройство с това име.");
        }

        type.Name = name;
        type.IsActive = dto.IsActive;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// „Изтриването“ е меко: IsActive = false. Заявките и техниците сочат към вида,
    /// затова редът остава, а старите заявки още показват името му.
    /// Неактивен вид не се предлага и не се приема за нови заявки (400).
    /// Един UPDATE ... WHERE Id AND IsActive = 1; повторно изтриване -> 0 реда -> 404.
    /// </summary>
    public async Task DeactivateAsync(int id)
    {
        var changed = await db.DeviceTypes
            .Where(d => d.DeviceTypeId == id && d.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.IsActive, false));

        if (changed == 0)
        {
            throw AppException.NotFound("Видът устройство не съществува или вече е изтрит.");
        }
    }
}
