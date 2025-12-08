using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
    private readonly api.Program.AppOptions _opts; 

    public AuthController(ApplicationDbContext db, IOptions<api.Program.AppOptions> opts)
    {
        _db = db;
        _opts = opts.Value;
    }
    
    public sealed record AdminLoginRequest(string Email, string Password);
    public sealed record JwtResponse(string Token);
    public sealed record PlayerRegisterRequest(string Name, string Email, string Password, string? Phone);
    public sealed record PlayerLoginRequest(string Email, string Password);

    // --- Admin login ---
    [HttpPost("admin/login")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtResponse>> AdminLogin([FromBody] AdminLoginRequest dto)
    {
        var admin = await _db.admins.FirstOrDefaultAsync(a => a.email == dto.Email);
        if (admin is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(admin.password_hash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, admin.password_hash))
            return Unauthorized();

        return Ok(new JwtResponse(CreateJwt(
            subject: admin.id.ToString(),
            email: admin.email,
            roles: new[] { "admin" })));
    }
    
    [HttpPost("player/register")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtResponse>> PlayerRegister([FromBody] PlayerRegisterRequest dto)
    {
        var exists = await _db.player_credentials.AnyAsync(pc => pc.email == dto.Email);
        if (exists) return Conflict("Email already registered.");

        var p = new players
        {
            name = dto.Name,
            email = dto.Email,
            phone = dto.Phone,
            is_active = true,
            created_at = DateTime.UtcNow
        };
        _db.players.Add(p);
        await _db.SaveChangesAsync();

        _db.player_credentials.Add(new player_credentials
        {
            player_id = p.id,
            email = dto.Email,
            password_hash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            created_at = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(new JwtResponse(CreateJwt(
            subject: p.id.ToString(),
            email: dto.Email,
            roles: new[] { "player" })));
    }

    [HttpPost("player/login")]
    [AllowAnonymous]
    public async Task<ActionResult<JwtResponse>> PlayerLogin([FromBody] PlayerLoginRequest dto)
    {
        var cred = await _db.player_credentials.FirstOrDefaultAsync(pc => pc.email == dto.Email);
        if (cred is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(cred.password_hash) ||
            !BCrypt.Net.BCrypt.Verify(dto.Password, cred.password_hash))
            return Unauthorized();

        var p = await _db.players.FirstOrDefaultAsync(x => x.id == cred.player_id && !x.is_deleted);
        if (p is null) return Unauthorized();

        return Ok(new JwtResponse(CreateJwt(
            subject: p.id.ToString(),
            email: cred.email,
            roles: new[] { "player" })));
    }

    [HttpGet("whoami")]
    [Authorize]
    public ActionResult<object> WhoAmI()
        => Ok(new
        {
            sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            email = User.FindFirstValue(JwtRegisteredClaimNames.Email),
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
        });

    private string CreateJwt(string subject, string email, string[] roles)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, subject),
            new(JwtRegisteredClaimNames.Email, email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: null, audience: null,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
