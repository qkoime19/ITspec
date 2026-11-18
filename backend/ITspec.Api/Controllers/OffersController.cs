using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>
/// Действията с оферта са POST към под-адрес (/accept, /withdraw), а не PUT със статус в тялото:
/// така клиентът не може да изпрати произволен статус.
/// </summary>
[ApiController]
[Route("api/v1/offers")]
[Authorize]
public class OffersController(OfferService offers) : ControllerBase
{
    /// <summary>„Моите оферти“ на техника.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<ActionResult<List<MyOfferDto>>> GetMine()
    {
        return Ok(await offers.GetMineAsync(User.GetUserId()));
    }

    /// <summary>Приемане през usp_AcceptOffer. 200 / 409 (чужда заявка, не е Open, техникът е зает...).</summary>
    [HttpPost("{id:int}/accept")]
    [Authorize(Roles = Roles.Client)]
    public async Task<ActionResult<AcceptOfferResultDto>> Accept(int id)
    {
        return Ok(await offers.AcceptAsync(id, User.GetUserId()));
    }

    /// <summary>Оттегляне. 204 / 403 (чужда) / 409 (не чака).</summary>
    [HttpPost("{id:int}/withdraw")]
    [Authorize(Roles = Roles.Technician)]
    public async Task<IActionResult> Withdraw(int id)
    {
        await offers.WithdrawAsync(id, User.GetUserId());
        return NoContent();
    }
}
