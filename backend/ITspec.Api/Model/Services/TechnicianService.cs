using System.Data;
using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>Профили на техниците и смяна на видовете устройства на техник.</summary>
public class TechnicianService(AppDbContext db)
{
    /// <summary>
    /// Публичното търсене чете от изгледа vw_TechnicianProfiles (той вече скрива блокираните
    /// и няма телефон). Филтрите се добавят към заявката само ако са подадени — накрая една SQL заявка.
    /// </summary>
    public async Task<List<TechnicianProfileDto>> SearchAsync(int? deviceTypeId, string? city, int page)
    {
        var query = db.TechnicianProfiles.AsQueryable();

        if (deviceTypeId is not null)
        {
            query = query.Where(p => db.TechnicianDeviceTypes
                .Any(t => t.TechnicianId == p.TechnicianId && t.DeviceTypeId == deviceTypeId));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var c = city.Trim();
            query = query.Where(p => p.City.Contains(c));
        }

        return await query
            .OrderByDescending(p => p.AvgRating)
            .ThenBy(p => p.FullName)
            .Skip((Math.Max(page, 1) - 1) * RequestService.PageSize)
            .Take(RequestService.PageSize)
            .Select(p => new TechnicianProfileDto(
                p.TechnicianId, p.FullName, p.City, p.DeviceTypes, p.AvgRating, p.ReviewsCount, p.CompletedCount))
            .ToListAsync();
    }

    /// <summary>Профилът на текущия техник (1:1 — данните са в Users, затова няма отделна таблица).</summary>
    public async Task<MyProfileDto> GetMyProfileAsync(int technicianId)
    {
        return await db.Users
            .Where(u => u.UserId == technicianId)
            .Select(u => new MyProfileDto(u.FullName, u.Email, u.Phone, u.City, u.AvgRating, u.ReviewsCount))
            .SingleOrDefaultAsync()
            ?? throw AppException.NotFound("Потребителят не съществува.");
    }

    /// <summary>Промяна на профила: име, телефон, град. Имейлът, ролята и рейтингът не се сменят оттук.</summary>
    public async Task UpdateMyProfileAsync(int technicianId, UpdateProfileDto dto)
    {
        var changed = await db.Users
            .Where(u => u.UserId == technicianId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(u => u.FullName, dto.FullName.Trim())
                .SetProperty(u => u.Phone, dto.Phone.Trim())
                .SetProperty(u => u.City, dto.City.Trim()));

        if (changed == 0)
        {
            throw AppException.NotFound("Потребителят не съществува.");
        }
    }

    public async Task<TechnicianDetailsDto> GetByIdAsync(int technicianId)
    {
        var profile = await db.TechnicianProfiles
            .Where(p => p.TechnicianId == technicianId)
            .Select(p => new TechnicianProfileDto(
                p.TechnicianId, p.FullName, p.City, p.DeviceTypes, p.AvgRating, p.ReviewsCount, p.CompletedCount))
            .SingleOrDefaultAsync()
            ?? throw AppException.NotFound("Техникът не съществува.");

        var reviews = await db.Reviews
            .Where(r => r.SupportRequest.Offers.Any(o => o.TechnicianId == technicianId && o.Status == OfferStatus.Accepted))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new TechnicianReviewDto(r.Rating, r.Comment, r.SupportRequest.Title, r.CreatedAt))
            .ToListAsync();

        return new TechnicianDetailsDto(profile, reviews);
    }

    public async Task<List<int>> GetMyDeviceTypeIdsAsync(int technicianId)
    {
        return await db.TechnicianDeviceTypes
            .Where(t => t.TechnicianId == technicianId)
            .Select(t => t.DeviceTypeId)
            .ToListAsync();
    }

    /// <summary>
    /// Смяна на всички видове устройства наведнъж през процедурата usp_SetTechnicianDeviceTypes.
    /// Списъкът се подава като табличен тип dbo.IdListType (SqlDbType.Structured) — затова ADO.NET параметър.
    /// </summary>
    public async Task SetMyDeviceTypesAsync(int technicianId, IEnumerable<int> deviceTypeIds)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(int));
        foreach (var id in deviceTypeIds.Distinct())
        {
            table.Rows.Add(id);
        }

        var technicianParam = new SqlParameter("@TechnicianId", SqlDbType.Int) { Value = technicianId };
        var idsParam = new SqlParameter("@DeviceTypeIds", SqlDbType.Structured)
        {
            TypeName = "dbo.IdListType",
            Value = table
        };

        await db.Database.ExecuteSqlRawAsync(
            "EXEC dbo.usp_SetTechnicianDeviceTypes @TechnicianId, @DeviceTypeIds",
            technicianParam, idsParam);
    }
}
