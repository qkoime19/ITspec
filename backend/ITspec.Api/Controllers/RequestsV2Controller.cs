using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>
/// Версия 2 на списъка със заявки (стъпка 10е). Чупеща промяна спрямо v1:
///   1) отговорът е обект { items, page, pageSize, total }, а не гол масив;
///   2) видът устройство е вложен обект { id, name }, а не низ deviceTypeName.
/// Сервизът е общ с v1 (RequestService) — логиката не се копира.
/// </summary>
[ApiController]
[Route("api/v2/requests")]
[Authorize]
public class RequestsV2Controller(RequestService requests) : ControllerBase
{
    /// <summary>
    /// GET /api/v2/requests?status=Open&amp;deviceTypeId=1&amp;page=1&amp;pageSize=20
    /// Същите правила като v1: клиентът — своите, техникът — отворените в своите видове, Admin — всички.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<RequestListItemV2Dto>>> GetPage(
        [FromQuery] RequestStatus? status,
        [FromQuery] int? deviceTypeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = RequestService.PageSize)
    {
        return Ok(await requests.GetPageV2Async(User.GetUserId(), User.GetRole(), status, deviceTypeId, page, pageSize));
    }
}
