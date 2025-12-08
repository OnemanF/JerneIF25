using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using JerneIF25.DataAccess.Entities;
using api.Etc;

namespace api.Controllers;

[ApiController]
[Route("boards")]
public class BoardsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISieveProcessor _sieve;
    private readonly TimeProvider _tp;

    public BoardsController(ApplicationDbContext db, ISieveProcessor sieve, TimeProvider tp)
    {
        _db = db;
        _sieve = sieve;
        _tp = tp;
    }
    
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieve)
    {
        var q = _db.boards
                   .AsNoTracking()
                   .Where(b => !b.is_deleted);

        q = _sieve.Apply(sieve, q);
        var list = await q.ToListAsync();
        return Ok(list);
    }
    
    // playerId tages fra JWT
    public sealed record CreateBoardDto(int[] Numbers, long GameId, int RepeatGames);

    [HttpPost]
    [Authorize(Roles = "player")]
    public async Task<IActionResult> Create([FromBody] CreateBoardDto dto)
    {
        BoardPricing.EnsureValidNumbers(dto.Numbers);
        
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub) || !long.TryParse(sub, out var playerId))
            return Unauthorized("Kun spillere kan købe kuponer.");

        // Tjek spiller
        var player = await _db.players.AsNoTracking()
            .FirstOrDefaultAsync(p => p.id == playerId && !p.is_deleted);
        if (player is null) return BadRequest("Player not found.");
        if (!player.is_active) return BadRequest("Player is inactive.");

        // Tjek aktivt spil
        var game = await _db.games
            .FirstOrDefaultAsync(g => g.id == dto.GameId && g.status == "active" && !g.is_deleted);
        if (game is null) return BadRequest("Active game not found.");

        // Håndhæv cutoff (lørdag 17:00 dansk tid)
        if (TimeAndCalendar.CutoffPassed(_tp, game.week_start))
            return BadRequest("Køb lukket for denne uge (efter lørdag kl. 17).");

        // Pris ud fra antal tal
        var price = BoardPricing.PriceForCount(dto.Numbers.Length);
        
        var entity = new boards
        {
            game_id      = dto.GameId,
            player_id    = playerId,
            numbers      = dto.Numbers.Select(n => (short)n).ToList(),
            price_dkk    = price,
            purchased_at = DateTime.UtcNow,
            created_at   = DateTime.UtcNow
        };

        _db.boards.Add(entity);

        // gentagelse (maks 52 uger)
        var repeat = Math.Max(0, Math.Min(dto.RepeatGames, 52));
        if (repeat > 0)
        {
            _db.board_subscriptions.Add(new board_subscriptions
            {
                player_id       = playerId,
                numbers         = dto.Numbers.Select(n => (short)n).ToList(),
                remaining_weeks = repeat,
                is_active       = true,
                started_at      = DateTime.UtcNow,
                created_at      = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();
        return Ok(entity);
    }
}
