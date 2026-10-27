using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>Действия на администратора, които не принадлежат на друг ресурс.</summary>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = Roles.Admin)]
public class AdminController(AdminService admin) : ControllerBase
{
    [HttpGet("users")]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers()
    {
        return Ok(await admin.GetUsersAsync());
    }

    [HttpPost("users/{id:int}/block")]
    public async Task<IActionResult> Block(int id)
    {
        await admin.SetBlockedAsync(id, true);
        return NoContent();
    }

    [HttpPost("users/{id:int}/unblock")]
    public async Task<IActionResult> Unblock(int id)
    {
        await admin.SetBlockedAsync(id, false);
        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<StatsDto>> GetStats()
    {
        return Ok(await admin.GetStatsAsync());
    }
}
