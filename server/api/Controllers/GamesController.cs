using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JerneIF25.DataAccess.Entities;
using api.Etc;

namespace api.Controllers;

[ApiController]
[Route("games")]
public class GamesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _tp;

    public GamesController(ApplicationDbContext db, TimeProvider tp)
    {
        _db = db;
        _tp = tp;
    }
    
    private static DateOnly NextWeek(DateOnly weekStart) => weekStart.AddDays(7);
    
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> List([FromQuery] string? status)
    {
        var q = _db.games.AsNoTracking().Where(g => !g.is_deleted);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            if (s is "active" or "closed" or "inactive")
                q = q.Where(g => g.status == s);
        }
        else
        {
            q = q.Where(g => g.status == "active" || g.status == "closed");
        }

        var rows = await q
            .OrderByDescending(g => g.week_start)
            .Select(g => new
            {
                id = g.id,
                week_start = g.week_start,
                status = g.status,
                winning = g.winning_nums,
                revenueDkk = _db.boards
                    .Where(b => !b.is_deleted && b.game_id == g.id)
                    .Select(b => (decimal?)b.price_dkk)
                    .Sum() ?? 0m
            })
            .ToListAsync();

        return Ok(rows);
    }
    //admin
    public sealed record BoardRowDto(
        long Id,
        long PlayerId,
        string PlayerName,
        decimal PriceDkk,
        short[] Numbers,     
        bool IsWinner
    );

    public sealed record GameBoardsResponse(
        long GameId,
        int WinnersTotal,
        int BoardsTotal,
        IEnumerable<BoardRowDto> Boards
    );
    
    [HttpGet("{id:long}/boards")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> BoardsForGame(long id)
    {
        var g = await _db.games.AsNoTracking().FirstOrDefaultAsync(x => x.id == id && !x.is_deleted);
        if (g is null) return NotFound("Uge ikke fundet.");

        var win = (g.winning_nums ?? new List<short>()).ToArray();

        var rows = await _db.boards
            .Where(b => !b.is_deleted && b.game_id == id)
            .Join(_db.players.Where(p => !p.is_deleted),
                  b => b.player_id,
                  p => p.id,
                  (b, p) => new { b, p })
            .Select(x => new
            {
                x.b.id,
                x.b.price_dkk,
                Numbers = x.b.numbers, // List<short> in DB
                PlayerId = x.p.id,
                PlayerName = x.p.name
            })
            .ToListAsync();

        bool IsWinner(IEnumerable<short> nums) => win.Length == 3 && win.All(n => nums.Contains(n));

        var mapped = rows.Select(r => new BoardRowDto(
            Id: r.id,
            PlayerId: r.PlayerId,
            PlayerName: r.PlayerName,
            PriceDkk: r.price_dkk,
            Numbers: (r.Numbers ?? new List<short>()).ToArray(),
            IsWinner: IsWinner(r.Numbers ?? new List<short>())
        )).ToList();

        var resp = new GameBoardsResponse(
            GameId: id,
            WinnersTotal: mapped.Count(x => x.IsWinner),
            BoardsTotal: mapped.Count,
            Boards: mapped
        );

        return Ok(resp);
    }

    public sealed record GameSummaryDto(long Id, int WinnersTotal, int BoardsTotal);
    
    [HttpGet("{id:long}/summary")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Summary(long id)
    {
        var g = await _db.games.AsNoTracking().FirstOrDefaultAsync(x => x.id == id && !x.is_deleted);
        if (g is null) return NotFound();

        var win = (g.winning_nums ?? new List<short>()).ToArray();

        var boards = await _db.boards
            .Where(b => !b.is_deleted && b.game_id == id)
            .Select(b => new { b.numbers })
            .ToListAsync();

        int winners = boards.Count(b =>
            win.Length == 3 && win.All(n => (b.numbers ?? new List<short>()).Contains(n)));

        return Ok(new GameSummaryDto(id, winners, boards.Count));
    }
    
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<IActionResult> Active()
    {
        var active = await _db.games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.status == "active" && !g.is_deleted);

        if (active is not null) return Ok(active);

        var ws = TimeAndCalendar.IsoWeekStartLocal(_tp.GetUtcNow());
        var existing = await _db.games.FirstOrDefaultAsync(g => g.week_start == ws && !g.is_deleted);

        if (existing is null)
        {
            var ng = new games
            {
                week_start = ws,
                status     = "active",
                created_at = DateTime.UtcNow
            };
            _db.games.Add(ng);
            await _db.SaveChangesAsync();
            return Ok(ng);
        }

        if (existing.status != "active")
        {
            existing.status     = "active";
            existing.updated_at = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return Ok(existing);
    }

    public sealed record StartGameDto(DateOnly? WeekStart);
    
    [HttpPost("start")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Start([FromBody] StartGameDto dto)
    {
        var active = await _db.games.FirstOrDefaultAsync(g => g.status == "active" && !g.is_deleted);
        if (active is not null)
        {
            active.status = "inactive";
            active.updated_at = DateTime.UtcNow;
        }

        var ws = dto.WeekStart ?? TimeAndCalendar.IsoWeekStartLocal(_tp.GetUtcNow());

        var g = await _db.games.FirstOrDefaultAsync(x => x.week_start == ws && !x.is_deleted);
        if (g is null)
        {
            g = new games
            {
                week_start = ws,
                status     = "active",
                created_at = DateTime.UtcNow
            };
            _db.games.Add(g);
        }
        else
        {
            g.status = "active";
            g.updated_at = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(g);
    }

    public sealed record PublishWinningNumbersDto(long GameId, int[] Numbers);
    
    [HttpPost("publish")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Publish([FromBody] PublishWinningNumbersDto dto)
    {
        if (dto.Numbers is null || dto.Numbers.Length != 3) return BadRequest("Præcis 3 tal kræves.");
        if (dto.Numbers.Any(n => n < 1 || n > 16)) return BadRequest("Tal skal være 1..16.");
        if (dto.Numbers.Distinct().Count() != 3) return BadRequest("Tal skal være unikke.");

        var g = await _db.games.FirstOrDefaultAsync(x =>
            x.id == dto.GameId && x.status == "active" && !x.is_deleted);

        if (g is null) return BadRequest("Aktiv uge ikke fundet.");

        g.winning_nums = dto.Numbers.Select(n => (short)n).ToList();
        g.status       = "closed";
        g.published_at = DateTime.UtcNow;
        g.updated_at   = DateTime.UtcNow;

        var nextWs = NextWeek(g.week_start);
        var next = await _db.games.FirstOrDefaultAsync(x => x.week_start == nextWs && !x.is_deleted);

        if (next is null)
        {
            next = new games
            {
                week_start = nextWs,
                status     = "active",
                created_at = DateTime.UtcNow
            };
            _db.games.Add(next);
        }
        else
        {
            next.status     = "active";
            next.updated_at = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            id = g.id,
            week_start = g.week_start,
            status = g.status,
            winning = g.winning_nums
        });
    }
    
    public sealed record SaveDraftDto(long GameId, int[] Numbers);
    
    [HttpPost("draft")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SaveDraft([FromBody] SaveDraftDto dto)
    {
        if (dto.Numbers is null || dto.Numbers.Length != 3) return BadRequest("Præcis 3 tal kræves.");
        if (dto.Numbers.Any(n => n < 1 || n > 16)) return BadRequest("Tal skal være 1..16.");
        if (dto.Numbers.Distinct().Count() != 3) return BadRequest("Tal skal være unikke.");

        var g = await _db.games.FirstOrDefaultAsync(x => x.id == dto.GameId && !x.is_deleted);
        if (g is null) return NotFound("Uge ikke fundet.");
        if (g.status == "closed") return BadRequest("Kan ikke gemme på lukket uge.");
        if (g.status != "active") return BadRequest("Kun aktiv uge kan få udkast.");

        g.winning_nums = dto.Numbers.Select(n => (short)n).ToList();
        g.updated_at   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(new { id = g.id, week_start = g.week_start, status = g.status, winning = g.winning_nums });
    }
    
    public sealed record UndoRequest(long? ClosedGameId);
    
    [HttpPost("undo")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Undo([FromBody] UndoRequest req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();
        
        var closed = req.ClosedGameId.HasValue
            ? await _db.games.FirstOrDefaultAsync(g => g.id == req.ClosedGameId && !g.is_deleted && g.status == "closed")
            : await _db.games.Where(g => !g.is_deleted && g.status == "closed")
                             .OrderByDescending(g => g.week_start)
                             .FirstOrDefaultAsync();

        if (closed is null) return BadRequest("Ingen lukket uge at fortryde.");
        
        var nextWs = NextWeek(closed.week_start);
        var next = await _db.games.FirstOrDefaultAsync(g => g.week_start == nextWs && !g.is_deleted);
        
        if (next is not null && await _db.boards.AnyAsync(b => !b.is_deleted && b.game_id == next.id))
            return BadRequest("Næste uge har allerede køb – fortryd ikke muligt.");
        
        var currentActive = await _db.games.FirstOrDefaultAsync(g => g.status == "active" && !g.is_deleted);
        
        if (currentActive is not null)
        {
            if (next is not null && currentActive.id == next.id)
            {
                next.is_deleted = true;
                next.updated_at = DateTime.UtcNow;
            }
            else
            {
                currentActive.status = "inactive";
                currentActive.updated_at = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        // Re-open the closed week
        closed.status       = "active";
        closed.published_at = null;
        closed.winning_nums = null;  
        closed.updated_at   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new
        {
            id = closed.id,
            week_start = closed.week_start,
            status = closed.status,
            winning = closed.winning_nums
        });
    }
}
