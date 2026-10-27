using ITspec.Api.Infrastructure.Errors;
using ITspec.Api.Model.Data;
using ITspec.Api.Model.Entities;
using ITspec.Api.View.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ITspec.Api.Model.Services;

/// <summary>Действия на администратора, които не принадлежат на друг ресурс.</summary>
public class AdminService(AppDbContext db)
{
    public async Task<List<AdminUserDto>> GetUsersAsync()
    {
        return await db.Users
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => new AdminUserDto(u.UserId, u.FullName, u.Email, u.City, u.Role, u.IsBlocked, u.CreatedAt))
            .ToListAsync();
    }

    /// <summary>Потребителите не се трият, а се блокират. Администратор не може да бъде блокиран.</summary>
    public async Task SetBlockedAsync(int userId, bool isBlocked)
    {
        var user = await db.Users.FindAsync(userId)
            ?? throw AppException.NotFound("Потребителят не съществува.");

        if (user.Role == Roles.Admin)
        {
            throw AppException.Conflict("Администратор не може да бъде блокиран.");
        }

        user.IsBlocked = isBlocked;
        await db.SaveChangesAsync();
    }

    public async Task<StatsDto> GetStatsAsync()
    {
        return new StatsDto(
            await db.Users.CountAsync(u => u.Role == Roles.Client),
            await db.Users.CountAsync(u => u.Role == Roles.Technician),
            await db.SupportRequests.CountAsync(r => r.Status == RequestStatus.Open),
            await db.SupportRequests.CountAsync(r => r.Status == RequestStatus.Assigned),
            await db.SupportRequests.CountAsync(r => r.Status == RequestStatus.Completed),
            await db.SupportRequests.CountAsync(r => r.Status == RequestStatus.Cancelled),
            await db.Offers.CountAsync(),
            await db.Reviews.CountAsync());
    }
}
