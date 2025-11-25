using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using JerneIF25.DataAccess.Entities;
using api.Models;
using api.Etc;

namespace api.Controllers;

[ApiController]
[Route("boards")]
public class BoardsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISieveProcessor _sieve;

    public BoardsController(ApplicationDbContext db, ISieveProcessor sieve)
    {
        _db = db;
        _sieve = sieve;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieve)
    {
        var query = _db.boards.AsNoTracking();
        query = _sieve.Apply(sieve, query);
        return Ok(await query.ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBoardDto dto)
    {
        BoardPricing.EnsureValidNumbers(dto.Numbers);
        
        var player = await _db.players.AsNoTracking().FirstOrDefaultAsync(p => p.id == dto.PlayerId && !p.is_deleted);
        if (player is null) return BadRequest("Player not found.");
        if (!player.is_active) return BadRequest("Player is inactive.");
        
        var game = await _db.games.AsNoTracking().FirstOrDefaultAsync(g => g.id == dto.GameId && g.status == "active" && !g.is_deleted);
        if (game is null) return BadRequest("Active game not found.");
        
        var price = BoardPricing.PriceForCount(dto.Numbers.Length);

        var entity = new board
        {
            game_id   = dto.GameId,
            player_id = dto.PlayerId,
            numbers   = dto.Numbers.Select(n => (short)n).ToList(), 
            price_dkk = price
        };

        _db.boards.Add(entity);
        await _db.SaveChangesAsync();
        return Ok(entity);
    }
}
