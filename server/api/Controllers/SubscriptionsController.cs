using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JerneIF25.DataAccess.Entities;

namespace api.Controllers;

[ApiController]
[Route("subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public SubscriptionsController(ApplicationDbContext db) => _db = db;

    [HttpGet("{playerId:long}")]
    public async Task<IActionResult> List(long playerId)
    {
        var subs = await _db.board_subscriptions.AsNoTracking()
            .Where(s => !s.is_deleted && s.player_id == playerId && s.is_active)
            .ToListAsync();
        return Ok(subs);
    }

    public sealed record CreateSubDto(long PlayerId, int[] Numbers, int RemainingWeeks);
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubDto dto)
    {
        if (dto.Numbers is null || dto.Numbers.Length < 5 || dto.Numbers.Length > 8)
            return BadRequest("Numbers must be 5–8.");

        var s = new board_subscriptions
        {
            player_id = dto.PlayerId,
            numbers = dto.Numbers.Select(n => (short)n).ToList(),
            remaining_weeks = Math.Max(0, Math.Min(dto.RemainingWeeks, 52)),
            is_active = true,
            started_at = DateTime.UtcNow,
            created_at = DateTime.UtcNow
        };
        _db.board_subscriptions.Add(s);
        await _db.SaveChangesAsync();
        return Ok(s);
    }

    public sealed record CancelSubDto(long SubscriptionId);
    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelSubDto dto)
    {
        var s = await _db.board_subscriptions.FirstOrDefaultAsync(x => x.id == dto.SubscriptionId && !x.is_deleted);
        if (s is null) return NotFound();
        s.is_active = false;
        s.canceled_at = DateTime.UtcNow;
        s.updated_at = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(s);
    }
}
