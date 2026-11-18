using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>
/// Всичко админско е на едно място: [Authorize(Roles = Admin)] над класа и няма [AllowAnonymous].
/// Така лесно се проверява, че нищо админско не е останало открито.
/// </summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(AdminService admin) : ControllerBase
{
    /// <summary>GET /api/v1/admin/users?role=Technician&amp;blocked=false&amp;page=1</summary>
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers(
        [FromQuery] string? role, [FromQuery] bool? blocked, [FromQuery] int page = 1)
    {
        return Ok(await admin.GetUsersAsync(role, blocked, page));
    }

    /// <summary>204 / 404 / 409 (себе си или друг администратор).</summary>
    [HttpPost("users/{id:int}/block")]
    public async Task<IActionResult> Block(int id)
    {
        await admin.SetBlockedAsync(id, true, User.GetUserId());
        return NoContent();
    }

    [HttpPost("users/{id:int}/unblock")]
    public async Task<IActionResult> Unblock(int id)
    {
        await admin.SetBlockedAsync(id, false, User.GetUserId());
        return NoContent();
    }

    /// <summary>Всички заявки: GET /api/v1/admin/requests?status=Open&amp;page=1</summary>
    [HttpGet("requests")]
    public async Task<ActionResult<List<RequestListItemDto>>> GetRequests(
        [FromQuery] RequestStatus? status, [FromQuery] int page = 1)
    {
        return Ok(await admin.GetRequestsAsync(status, page));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<StatsDto>> GetStats()
    {
        return Ok(await admin.GetStatsAsync());
    }
}

// Госпожо, как е кода?
// камерата снима и аз пиша, :D