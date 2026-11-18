using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>
/// Заявки. Контролерът е тънък: проверява ролята и вика Service.
/// „Само своя заявка“ се проверява в Service / процедурата, не с роля.
/// </summary>
[ApiController]
[Route("api/v1/requests")]
[Authorize]
public class RequestsController(RequestService requests) : ControllerBase
{
    /// <summary>Списък според ролята.</summary>
    [HttpGet]
    public async Task<ActionResult<List<RequestListItemDto>>> GetAll()
    {
        return Ok(await requests.GetListAsync(User.GetUserId(), User.GetRole()));
    }

    /// <summary>
    /// „Моите заявки“ на клиента. Собственикът е от токена; ?status=Open и ?page=2 са по избор.
    /// Пример: GET /api/v1/requests/mine?status=Open&amp;page=1
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<List<RequestListItemDto>>> GetMine([FromQuery] RequestStatus? status, [FromQuery] int page = 1)
    {
        return Ok(await requests.GetMineAsync(User.GetUserId(), status, page));
    }

    /// <summary>
    /// Публичните обяви (отворените заявки) за началната страница. Без вход.
    /// Пример: GET /api/v1/requests/open?deviceTypeId=1&amp;city=Враца&amp;search=екран
    /// </summary>
    [HttpGet("open")]
    [AllowAnonymous]
    public async Task<ActionResult<List<RequestListItemDto>>> GetOpen(
        [FromQuery] int? deviceTypeId, [FromQuery] string? city, [FromQuery] string? search)
    {
        return Ok(await requests.GetOpenPublicAsync(deviceTypeId, city, search));
    }

    /// <summary>Детайли. 200 / 404 (няма я или е чужда).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RequestDetailsDto>> GetById(int id)
    {
        return Ok(await requests.GetDetailsAsync(id, User.GetUserId(), User.GetRole()));
    }

    /// <summary>Нова заявка. 201 + Location / 400.</summary>
    [HttpPost]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<RequestDetailsDto>> Create(CreateRequestDto dto)
    {
        var userId = User.GetUserId();
        var id = await requests.CreateAsync(userId, dto);
        var created = await requests.GetDetailsAsync(id, userId, Roles.Client);
        return CreatedAtAction(nameof(GetById), new { id }, created);
    }

    /// <summary>
    /// Редакция на съдържанието, само докато заявката е Open.
    /// 204 / 400 (валидация, неактивен вид) / 404 (няма я или е чужда) / 409 (не е Open).
    /// Статусът НЕ се сменя оттук — само с действията accept, cancel и complete.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> Update(int id, UpdateRequestDto dto)
    {
        await requests.UpdateAsync(id, User.GetUserId(), dto);
        return NoContent();
    }

    /// <summary>Прекратяване. 204 / 404 (няма я или е чужда) / 409 (грешен статус).</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = Roles.Client)]
    public async Task<IActionResult> Cancel(int id)
    {
        await requests.CancelAsync(id, User.GetUserId());
        return NoContent();
    }

}
