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
    /// <summary>Публичният списък идва от изгледа vw_TechnicianProfiles.</summary>
    public async Task<List<TechnicianProfileDto>> GetAllAsync()
    {
        return await db.TechnicianProfiles
            .OrderByDescending(p => p.AvgRating)
            .ThenBy(p => p.FullName)
            .Select(p => new TechnicianProfileDto(
                p.TechnicianId, p.FullName, p.City, p.DeviceTypes, p.AvgRating, p.ReviewsCount, p.CompletedCount))
            .ToListAsync();
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
