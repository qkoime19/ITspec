using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>
/// Тънък контролер (C): приема заявката, вика сервиза, връща статус код. Няма логика.
/// [ApiController] сам връща 400 при невалидно DTO.
/// </summary>
[ApiController]
[Route("api/v1/device-types")]
public class DeviceTypesController(DeviceTypeService deviceTypes) : ControllerBase
{
    /// <summary>Списък. 200.</summary>
    [HttpGet]
    public ActionResult<List<DeviceTypeDto>> GetAll()
    {
        return Ok(deviceTypes.GetAll());
    }

    /// <summary>Един запис. 200 / 404.</summary>
    [HttpGet("{id:int}")]
    public ActionResult<DeviceTypeDto> GetById(int id)
    {
        var type = deviceTypes.GetById(id);
        return type is null ? NotFound() : Ok(type);
    }

    /// <summary>Създаване. 201 + Location / 400.</summary>
    [HttpPost]
    public ActionResult<DeviceTypeDto> Create(SaveDeviceTypeDto dto)
    {
        var created = deviceTypes.Create(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.DeviceTypeId }, created);
    }
}
