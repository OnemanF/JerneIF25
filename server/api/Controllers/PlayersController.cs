using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using JerneIF25.DataAccess.Entities;
using api.Models;

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

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieve)
    {
        var q = _sieve.Apply(sieve, _db.players.AsNoTracking().Where(p => !p.is_deleted));
        return Ok(await q.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlayerDto dto)
    {
        var p = new player
        {
            name = dto.Name,
            phone = dto.Phone,
            email = dto.Email,
            is_active = false
        };
        _db.players.Add(p);
        await _db.SaveChangesAsync();
        return Ok(p);
    }

    [HttpPut("{id:long}")]
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
