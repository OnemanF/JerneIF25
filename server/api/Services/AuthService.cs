using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.DTOs.Auth;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api.Services;

public sealed class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly Program.AppOptions _opts;

    public AuthService(ApplicationDbContext db, IOptions<Program.AppOptions> opts)
    {
        _db = db;
        _opts = opts.Value;
    }

    public async Task<JwtResponse> AdminLoginAsync(AdminLoginRequest req)
    {
        var admin = await _db.admins.FirstOrDefaultAsync(a => a.email == req.Email);
        if (admin is null || string.IsNullOrWhiteSpace(admin.password_hash) ||
            !BCrypt.Net.BCrypt.Verify(req.Password, admin.password_hash))
            throw new InvalidOperationException("Invalid credentials.");

        var token = CreateJwt(subject: admin.id.ToString(), email: admin.email, roles: new[] { "admin" });
        return new JwtResponse(token);
    }

    public async Task<JwtResponse> PlayerRegisterAsync(PlayerRegisterRequest req)
    {
        var exists = await _db.player_credentials.AnyAsync(pc => pc.email == req.Email);
        if (exists) throw new InvalidOperationException("Email already registered.");

        var p = new players
        {
            name       = req.Name,
            email      = req.Email,
            phone      = req.Phone,
            is_active  = true,
            is_deleted = false,
            created_at = DateTime.UtcNow
        };
        _db.players.Add(p);
        await _db.SaveChangesAsync();

        _db.player_credentials.Add(new player_credentials
        {
            player_id     = p.id,
            email         = req.Email,
            password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            created_at    = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var token = CreateJwt(subject: p.id.ToString(), email: req.Email, roles: new[] { "player" });
        return new JwtResponse(token);
    }

    public async Task<JwtResponse> PlayerLoginAsync(PlayerLoginRequest req)
    {
        var cred = await _db.player_credentials.FirstOrDefaultAsync(pc => pc.email == req.Email);
        if (cred is null || string.IsNullOrWhiteSpace(cred.password_hash) ||
            !BCrypt.Net.BCrypt.Verify(req.Password, cred.password_hash))
            throw new InvalidOperationException("Invalid credentials.");

        var p = await _db.players.FirstOrDefaultAsync(x => x.id == cred.player_id && !x.is_deleted);
        if (p is null) throw new InvalidOperationException("Invalid credentials.");

        var token = CreateJwt(subject: p.id.ToString(), email: cred.email, roles: new[] { "player" });
        return new JwtResponse(token);
    }

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
