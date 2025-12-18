using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using api.DTOs.Auth;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("admin/login")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtResponse>> AdminLogin([FromBody] AdminLoginRequest dto)
    {
        try
        {
            var res = await _auth.AdminLoginAsync(dto);
            return Ok(res);
        }
        catch
        {
            return Unauthorized();
        }
    }

    [HttpPost("player/register")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtResponse>> PlayerRegister([FromBody] PlayerRegisterRequest dto)
    {
        try
        {
            var res = await _auth.PlayerRegisterAsync(dto);
            return Ok(res);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already", StringComparison.OrdinalIgnoreCase))
        {
            return Conflict("Email already registered.");
        }
    }

    [HttpPost("player/login")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtResponse>> PlayerLogin([FromBody] PlayerLoginRequest dto)
    {
        try
        {
            var res = await _auth.PlayerLoginAsync(dto);
            return Ok(res);
        }
        catch
        {
            return Unauthorized();
        }
    }

    [HttpGet("whoami")]
    [Authorize]
    public ActionResult<object> WhoAmI()
        => Ok(new
        {
            sub   = User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            email = User.FindFirstValue(JwtRegisteredClaimNames.Email),
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        });
}