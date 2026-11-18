using ITspec.Api.Model.Data;
using ITspec.Api.View.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>
/// Стъпка 7: същият сервиз, но чете от базата (EF Core). Контролерът почти не се промени.
/// </summary>
public class DeviceTypeService(AppDbContext db)
{
    /// <summary>Само активните, подредени по име. Броят заявки е в същата SQL заявка (проекция).</summary>
    public async Task<List<DeviceTypeDto>> GetAllAsync()
    {
        return await db.DeviceTypes
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new DeviceTypeDto(d.DeviceTypeId, d.Name, d.IsActive, d.SupportRequests.Count))
            .ToListAsync();
    }

    public async Task<DeviceTypeDto?> GetByIdAsync(int id)
    {
        return await db.DeviceTypes
            .Where(d => d.DeviceTypeId == id && d.IsActive)
            .Select(d => new DeviceTypeDto(d.DeviceTypeId, d.Name, d.IsActive, d.SupportRequests.Count))
            .SingleOrDefaultAsync();
    }

    public async Task<DeviceTypeDto> CreateAsync(SaveDeviceTypeDto dto)
    {
        var type = new Entities.DeviceType { Name = dto.Name.Trim(), IsActive = dto.IsActive };
        db.DeviceTypes.Add(type);
        await db.SaveChangesAsync();

        // Id е 0 до SaveChangesAsync: стойността идва от IDENTITY колоната в базата.
        return new DeviceTypeDto(type.DeviceTypeId, type.Name, type.IsActive, 0);
    }
}
