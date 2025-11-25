using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Models;
using JerneIF25.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly AppOptions _opts;

    public AuthController(ApplicationDbContext db, IOptions<AppOptions> opts)
    {
        _db = db;
        _opts = opts.Value;
    }

    public sealed record LoginRequest(string Email, string Password);
    public sealed record JwtResponse(string Token);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtResponse>> Login([FromBody] LoginRequest dto)
    {
        var admin = await _db.admins.FirstOrDefaultAsync(a => a.Email == dto.Email || a.Email == dto.Email);
        if (admin == null)
            return Unauthorized();

        var storedHash =
            (admin as dynamic).PasswordHash ?? 
            (admin as dynamic).password_hash;   

        if (string.IsNullOrWhiteSpace(storedHash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, (string)storedHash))
            return Unauthorized();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "admin"),
            new(JwtRegisteredClaimNames.Sub,
                ((admin as dynamic).Id ?? (admin as dynamic).id).ToString()),
            new(JwtRegisteredClaimNames.Email,
                (string)((admin as dynamic).Email ?? (admin as dynamic).email))
        };

        var token = new JwtSecurityToken(
            issuer: null,
            audience: null,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(new JwtResponse(jwt));
    }
    
    [HttpGet("whoami")]
    [Authorize(Roles = "admin")]
    public ActionResult<object> WhoAmI()
        => Ok(new
        {
            user = User.Identity?.Name,
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        });
}
