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
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TechnicianProfileDto>>> GetAll()
    {
        return Ok(await technicians.GetAllAsync());
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
