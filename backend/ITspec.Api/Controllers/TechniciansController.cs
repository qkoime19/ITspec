using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>Профили на техниците (публични) и видовете устройства на текущия техник.</summary>
[ApiController]
[Route("api/v1/technicians")]
public class TechniciansController(TechnicianService technicians) : ControllerBase
{
    /// <summary>
    /// Публично търсене (без вход): GET /api/v1/technicians?deviceTypeId=1&amp;city=Враца&amp;page=1.
    /// Всички филтри са по избор. Без телефон, имейл и блокирани техници.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TechnicianProfileDto>>> Search(
        [FromQuery] int? deviceTypeId, [FromQuery] string? city, [FromQuery] int page = 1)
    {
        return Ok(await technicians.SearchAsync(deviceTypeId, city, page));
    }

    /// <summary>Моят профил. Без id в адреса: „моят“ идва от токена.</summary>
    [HttpGet("me/profile")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<ActionResult<MyProfileDto>> GetMyProfile()
    {
        return Ok(await technicians.GetMyProfileAsync(User.GetUserId()));
    }

    /// <summary>Промяна на моя профил. 204 / 400.</summary>
    [HttpPut("me/profile")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto dto)
    {
        await technicians.UpdateMyProfileAsync(User.GetUserId(), dto);
        return NoContent();
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<TechnicianDetailsDto>> GetById(int id)
    {
        return Ok(await technicians.GetByIdAsync(id));
    }

    [HttpGet("me/device-types")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<ActionResult<List<int>>> GetMyDeviceTypes()
    {
        return Ok(await technicians.GetMyDeviceTypeIdsAsync(User.GetUserId()));
    }

    /// <summary>Смяна на всички видове наведнъж (табличен тип). 204 / 409 (невалиден списък).</summary>
    [HttpPut("me/device-types")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> SetMyDeviceTypes(SetDeviceTypesDto dto)
    {
        await technicians.SetMyDeviceTypesAsync(User.GetUserId(), dto.DeviceTypeIds);
        return NoContent();
    }
}
