using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JerneIF25.DataAccess.Entities;
using api.DTOs.Games;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("games")]
public class GamesController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IGamesService _svc;

    public GamesController(ApplicationDbContext db, IGamesService svc)
    {
        _db  = db;
        _svc = svc;
    }

    private static GameResponse Map(games g) =>
        new(g.id, g.week_start, g.status, g.winning_nums);
    
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<IEnumerable<GameListRowDto>>> List([FromQuery] string? status)
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
            .Select(g => new GameListRowDto(
                g.id,
                g.week_start,
                g.status,
                g.winning_nums,
                _db.boards
                    .Where(b => !b.is_deleted && b.game_id == g.id)
                    .Select(b => (decimal?)b.price_dkk)
                    .Sum() ?? 0m
            ))
            .ToListAsync();

        return Ok(rows);
    }
    
    [HttpGet("{id:long}/boards")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GameBoardsResponse>> BoardsForGame(long id)
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
                Numbers = x.b.numbers,
                PlayerId = x.p.id,
                PlayerName = x.p.name
            })
            .ToListAsync();

        bool IsWinner(IEnumerable<short> nums) => win.Length == 3 && win.All(n => nums.Contains(n));

        var mapped = rows.Select(r => new BoardRowDto(
            r.id, r.PlayerId, r.PlayerName, r.price_dkk,
            (r.Numbers ?? new List<short>()).ToArray(),
            IsWinner(r.Numbers ?? new List<short>())
        )).ToList();

        var resp = new GameBoardsResponse(id, mapped.Count(x => x.IsWinner), mapped.Count, mapped);
        return Ok(resp);
    }
    
    [HttpGet("{id:long}/summary")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GameSummaryDto>> Summary(long id)
    {
        var g = await _db.games.AsNoTracking().FirstOrDefaultAsync(x => x.id == id && !x.is_deleted);
        if (g is null) return NotFound();

        var win = (g.winning_nums ?? new List<short>()).ToArray();

        var boards = await _db.boards
            .Where(b => !b.is_deleted && b.game_id == id)
            .Select(b => new { b.numbers })
            .ToListAsync();

        int winners = boards.Count(b => win.Length == 3 && win.All(n => (b.numbers ?? new List<short>()).Contains(n)));

        return Ok(new GameSummaryDto(id, winners, boards.Count));
    }
    
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<GameResponse>> Active()
    {
        var g = await _svc.GetActiveAsync();
        if (g is null) return NotFound();
        return Ok(Map(g));
    }
    
    [HttpPost("start")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GameResponse>> Start([FromBody] StartGameRequest dto)
    {
        var g = await _svc.StartAsync(dto.WeekStart);
        return Ok(Map(g));
    }
    
    [HttpPost("publish")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GameResponse>> Publish([FromBody] PublishWinningNumbersRequest dto)
    {
        try
        {
            var (closed, _) = await _svc.PublishAsync(dto.GameId, dto.Numbers);
            return Ok(Map(closed));
        }
        catch (ArgumentException ex)        { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return BadRequest(ex.Message); }
    }
    
    [HttpPost("draft")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GameResponse>> SaveDraft([FromBody] SaveDraftRequest dto)
    {
        try
        {
            var g = await _svc.DraftAsync(dto.GameId, dto.Numbers);
            return Ok(Map(g));
        }
        catch (ArgumentException ex)        { return BadRequest(ex.Message); }
        catch (InvalidOperationException ex){ return BadRequest(ex.Message); }
    }
    
    [HttpPost("undo")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<GameResponse>> Undo([FromBody] UndoRequest req)
    {
        try
        {
            var g = await _svc.UndoAsync(req.ClosedGameId);
            return Ok(Map(g));
        }
        catch (InvalidOperationException ex){ return BadRequest(ex.Message); }
    }
}
