using ITspec.Api.Infrastructure.Auth;
using ITspec.Api.Model.Entities;
using ITspec.Api.Model.Services;
using ITspec.Api.View.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ITspec.Api.Controllers;

/// <summary>
/// Регистрация, вход, „кой съм аз“.
/// Ролята при регистрация се определя от адреса, а не от поле в тялото.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public class AuthController(AuthService auth) : ControllerBase
{
    /// <summary>Регистрация като клиент. 201 / 400 / 409 (зает имейл).</summary>
    [HttpPost("register/client")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> RegisterClient(RegisterDto dto)
    {
        var user = await auth.RegisterAsync(dto, Roles.Client);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Регистрация като техник. 201 / 400 / 409 (зает имейл).</summary>
    [HttpPost("register/technician")]
    [AllowAnonymous]
    public async Task<ActionResult<UserDto>> RegisterTechnician(RegisterDto dto)
    {
        var user = await auth.RegisterAsync(dto, Roles.Technician);
        return StatusCode(StatusCodes.Status201Created, user);
    }

    /// <summary>Вход. 200 + JWT / 401 (грешни данни) / 403 (блокиран).</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(LoginDto dto)
    {
        return Ok(await auth.LoginAsync(dto));
    }

    /// <summary>Текущият потребител по токена. 200 / 401 / 403 (блокиран).</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me()
    {
        return Ok(await auth.GetMeAsync(User.GetUserId()));
    }
}
