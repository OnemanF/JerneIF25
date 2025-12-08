using JerneIF25.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;

namespace api.Controllers;

[ApiController]
[Route("players")]
public class PlayersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISieveProcessor _sieve;

    public PlayersController(ApplicationDbContext db, ISieveProcessor sieve)
    {
        _db = db;
        _sieve = sieve;
    }

    public sealed record CreatePlayerDto(string Name, string? Phone, string? Email, bool? IsActive);
    public sealed record UpdatePlayerDto(string Name, string? Phone, string? Email, bool IsActive, DateOnly? MemberExpiresAt);

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieve)
    {
        var q = _db.players.AsNoTracking().Where(p => !p.is_deleted);
        q = _sieve.Apply(sieve, q);
        return Ok(await q.ToListAsync());
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] CreatePlayerDto dto)
    {
        var p = new players
        {
            name = dto.Name,
            phone = dto.Phone,
            email = dto.Email,
            is_active = dto.IsActive ?? false,
            created_at = DateTime.UtcNow
        };
        _db.players.Add(p);
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdatePlayerDto dto)
    {
        var p = await _db.players.FirstOrDefaultAsync(x => x.id == id && !x.is_deleted);
        if (p is null) return NotFound();

        p.name = dto.Name;
        p.phone = dto.Phone;
        p.email = dto.Email;
        p.is_active = dto.IsActive;
        p.member_expires_at = dto.MemberExpiresAt;
        p.updated_at = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SoftDelete(long id)
    {
        var p = await _db.players.FirstOrDefaultAsync(x => x.id == id && !x.is_deleted);
        if (p is null) return NotFound();

        p.is_deleted = true;
        p.updated_at = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
