using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>
/// Действията на администратора. Админът вижда всичко, но не е собственик:
/// не дава оферти, не приема и не прекратява от чуждо име.
/// </summary>
public class AdminService(AppDbContext db)
{
    private const int AdminPageSize = 50;

    /// <summary>Потребители с незадължителни филтри по роля и блокиране. Без PasswordHash.</summary>
    public async Task<List<AdminUserDto>> GetUsersAsync(string? role, bool? blocked, int page)
    {
        var query = db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        if (blocked is not null)
        {
            query = query.Where(u => u.IsBlocked == blocked);
        }

        return await query
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Skip((Math.Max(page, 1) - 1) * AdminPageSize)
            .Take(AdminPageSize)
            .Select(u => new AdminUserDto(u.UserId, u.FullName, u.Email, u.City, u.Role, u.IsBlocked, u.CreatedAt))
            .ToListAsync();
    }

    /// <summary>
    /// Блокиране / отблокиране с един UPDATE. Потребителите не се трият.
    /// Админ не може да блокира себе си, нито друг администратор (409) — иначе системата
    /// може да остане без администратор. Несъществуващ -> 404.
    /// </summary>
    public async Task SetBlockedAsync(int userId, bool isBlocked, int currentAdminId)
    {
        if (userId == currentAdminId)
        {
            throw AppException.Conflict("Не можете да блокирате себе си.");
        }

        var changed = await db.Users
            .Where(u => u.UserId == userId && u.Role != Roles.Admin)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.IsBlocked, isBlocked));

        if (changed == 0)
        {
            var exists = await db.Users.AnyAsync(u => u.UserId == userId);
            throw exists
                ? AppException.Conflict("Администратор не може да бъде блокиран.")
                : AppException.NotFound("Потребителят не съществува.");
        }
    }

    /// <summary>Всички заявки (на всички клиенти), с филтър по статус и страници.</summary>
    public async Task<List<RequestListItemDto>> GetRequestsAsync(RequestStatus? status, int page)
    {
        var query = db.SupportRequests.AsQueryable();
        if (status is not null)
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((Math.Max(page, 1) - 1) * AdminPageSize)
            .Take(AdminPageSize)
            .Select(r => new RequestListItemDto(
                r.SupportRequestId, r.Title, r.DeviceType.Name, r.DeviceModel, r.City, r.PreferredDate,
                r.Status, r.CreatedAt, r.Offers.Count(o => o.Status != OfferStatus.Withdrawn), r.Client.FullName))
            .ToListAsync();
    }

    /// <summary>
    /// Статистика с 4 SQL заявки: потребители по роля (една GroupBy), заявки по статус
    /// (една GroupBy), брой оферти, брой отзиви. Нищо не се зарежда в паметта, за да се брои.
    /// </summary>
    public async Task<StatsDto> GetStatsAsync()
    {
        var usersByRole = await db.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count);

        var requestsByStatus = await db.SupportRequests
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        return new StatsDto(
            usersByRole.GetValueOrDefault(Roles.Client),
            usersByRole.GetValueOrDefault(Roles.Technician),
            requestsByStatus.GetValueOrDefault(RequestStatus.Open),
            requestsByStatus.GetValueOrDefault(RequestStatus.Assigned),
            requestsByStatus.GetValueOrDefault(RequestStatus.Completed),
            requestsByStatus.GetValueOrDefault(RequestStatus.Cancelled),
            await db.Offers.CountAsync(),
            await db.Reviews.CountAsync());
    }
}
