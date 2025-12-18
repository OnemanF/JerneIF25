using api.DTOs.Players;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sieve.Models;
using Sieve.Services;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("players")]
public class PlayersController : ControllerBase
{
    private readonly IPlayersService _svc;
    private readonly ApplicationDbContext _db; 
    private readonly ISieveProcessor _sieve;

    public PlayersController(IPlayersService svc, ApplicationDbContext db, ISieveProcessor sieve)
    {
        _svc = svc;
        _db = db;
        _sieve = sieve;
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieve, CancellationToken ct)
    {
        var q = _db.players.AsNoTracking().Where(p => !p.is_deleted);
        q = _sieve.Apply(sieve, q);
        var rows = await q
            .OrderByDescending(p => p.created_at)
            .Select(p => new PlayerResponse
            {
                Id = p.id, Name = p.name, Phone = p.phone, Email = p.email,
                IsActive = p.is_active, MemberExpiresAt = p.member_expires_at,
                CreatedAt = p.created_at, UpdatedAt = p.updated_at
            })
            .ToListAsync(ct);

        return Ok(rows);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PlayerResponse>> Create([FromBody] CreatePlayerRequest dto, CancellationToken ct)
    {
        var res = await _svc.CreateAsync(dto, ct);
        return Ok(res);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<PlayerResponse>> Update(long id, [FromBody] UpdatePlayerRequest dto, CancellationToken ct)
    {
        try
        {
            var res = await _svc.UpdateAsync(id, dto, ct);
            return Ok(res);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SoftDelete(long id, CancellationToken ct)
    {
        try
        {
            await _svc.SoftDeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
