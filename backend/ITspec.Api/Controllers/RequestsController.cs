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
public class RequestsController(RequestService requests, OfferService offers) : ControllerBase
{
    /// <summary>
    /// Списък според ролята (v1, гол масив). Остаряла: новите клиенти ползват
    /// GET /api/v2/requests (страници + общ брой). v1 работи до края на учебната година.
    /// </summary>
    [HttpGet]
    [Obsolete("Използвайте GET /api/v2/requests.")]
    public async Task<ActionResult<List<RequestListItemDto>>> GetAll()
    {
        Response.Headers["Deprecation"] = "true";
        Response.Headers["Link"] = "</api/v2/requests>; rel=\"successor-version\"";
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

    /// <summary>Завършване. 204 / 404 (не е твоя работа) / 409 (не е Assigned).</summary>
    [HttpPost("{id:int}/complete")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> Complete(int id)
    {
        await requests.CompleteAsync(id, User.GetUserId());
        return NoContent();
    }

    /// <summary>Отзив. 201 / 400 (оценка) / 409 (не е Completed, чужда заявка, вече има отзив).</summary>
    [HttpPost("{id:int}/review")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<ReviewDto>> Review(int id, CreateReviewDto dto)
    {
        var review = await requests.AddReviewAsync(id, User.GetUserId(), dto);
        return StatusCode(StatusCodes.Status201Created, review);
    }

    /// <summary>Офертите по заявка. Само собственикът и Admin.</summary>
    [HttpGet("{id:int}/offers")]
    [Authorize(Roles = Roles.Client + "," + Roles.Admin)]
    public async Task<ActionResult<List<OfferDto>>> GetOffers(int id)
    {
        return Ok(await offers.GetForRequestAsync(id, User.GetUserId(), User.GetRole()));
    }

    /// <summary>Нова оферта. 201 / 403 (чужд вид устройство) / 409 (не е Open или вече има оферта).</summary>
    [HttpPost("{id:int}/offers")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<ActionResult<OfferDto>> CreateOffer(int id, CreateOfferDto dto)
    {
        var offer = await offers.CreateAsync(id, User.GetUserId(), dto);
        return StatusCode(StatusCodes.Status201Created, offer);
    }
}
