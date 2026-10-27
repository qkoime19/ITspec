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
            .Select(d => new DeviceTypeDto(d.DeviceTypeId, d.Name, d.IsActive))
            .ToListAsync();
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

        return new DeviceTypeDto(type.DeviceTypeId, type.Name, type.IsActive);
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
    /// „Изтриването“ е деактивиране (IsActive = false): заявките и техниците сочат към вида,
    /// затова редът остава. Неактивен вид не се предлага за нови заявки.
    /// </summary>
    public async Task DeactivateAsync(int id)
    {
        var type = await db.DeviceTypes.FindAsync(id)
            ?? throw AppException.NotFound("Видът устройство не съществува.");

        type.IsActive = false;
        await db.SaveChangesAsync();
    }
}
