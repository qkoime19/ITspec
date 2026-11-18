using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>Видовете устройства (категориите). Четат всички, променя само Admin.</summary>
[ApiController]
[Route("api/v1/device-types")]
[Authorize(Roles = Roles.Admin)]
public class DeviceTypesController(DeviceTypeService deviceTypes) : ControllerBase
{
    /// <summary>Активните видове. Admin може да поиска и неактивните с ?includeInactive=true.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<DeviceTypeDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var showInactive = includeInactive && User.IsInRole(Roles.Admin);
        return Ok(await deviceTypes.GetAllAsync(showInactive));
    }

    /// <summary>Един активен вид. 200 / 404 (няма го, изтрит е, или /abc).</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<DeviceTypeDto>> GetById(int id)
    {
        return Ok(await deviceTypes.GetByIdAsync(id));
    }

    /// <summary>Нов вид. 201 + Location: /api/v1/device-types/{id} / 400 / 409 (дублирано име).</summary>
    [HttpPost]
    public async Task<ActionResult<DeviceTypeDto>> Create(SaveDeviceTypeDto dto)
    {
        var created = await deviceTypes.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.DeviceTypeId }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, SaveDeviceTypeDto dto)
    {
        await deviceTypes.UpdateAsync(id, dto);
        return NoContent();
    }

    /// <summary>Меко изтриване (IsActive = false). 204 / 404 (няма го или вече е изтрит).</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await deviceTypes.DeactivateAsync(id);
        return NoContent();
    }
}
