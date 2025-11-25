using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JerneIF25.DataAccess.Entities;
using api.Models;

namespace api.Controllers;

[ApiController]
[Route("games")]
public class GamesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public GamesController(ApplicationDbContext db) => _db = db;

    [HttpGet("active")]
    public async Task<IActionResult> Active()
    {
        var g = await _db.games.AsNoTracking().FirstOrDefaultAsync(x => x.status == "active" && !x.is_deleted);
        return g is null ? NotFound() : Ok(g);
    }

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] PublishWinningNumbersDto dto)
    {
        if (dto.Numbers is null || dto.Numbers.Length != 3) return BadRequest("Exactly 3 numbers required.");
        if (dto.Numbers.Any(n => n < 1 || n > 16)) return BadRequest("Numbers must be 1..16.");
        if (dto.Numbers.Distinct().Count() != 3) return BadRequest("Numbers must be unique.");

        var g = await _db.games.FirstOrDefaultAsync(x => x.id == dto.GameId && x.status == "active" && !x.is_deleted);
        if (g is null) return BadRequest("Active game not found.");

        g.winning_nums = dto.Numbers.Select(n => (short)n).ToList();
        g.status = "closed";
        g.published_at = DateTime.UtcNow;
        g.updated_at = DateTime.UtcNow;
        
        await _db.SaveChangesAsync();
        return Ok(g);
    }
}