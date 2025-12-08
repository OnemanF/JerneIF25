using JerneIF25.DataAccess.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;

namespace api.Controllers;

[ApiController]
[Route("transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISieveProcessor _sieve;

    public TransactionsController(ApplicationDbContext db, ISieveProcessor sieve)
    {
        _db = db;
        _sieve = sieve;
    }

    public sealed record CreateTransactionDto(long PlayerId, decimal AmountDkk, string? MobilePayRef, string? Note);
    public sealed record DecideTransactionDto(long TransactionId, string Decision);

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieve)
    {
        var q = _db.transactions.AsNoTracking().Where(t => !t.is_deleted);
        q = _sieve.Apply(sieve, q);
        return Ok(await q.ToListAsync());
    }

    [HttpPost]
    [Authorize] 
    public async Task<IActionResult> Create([FromBody] CreateTransactionDto dto)
    {
        if (dto.AmountDkk <= 0) return BadRequest("Amount must be positive.");
        var playerExists = await _db.players.AnyAsync(p => p.id == dto.PlayerId && !p.is_deleted);
        if (!playerExists) return BadRequest("Player not found.");

        var tx = new transactions
        {
            player_id = dto.PlayerId,
            amount_dkk = dto.AmountDkk,
            mobilepay_ref = dto.MobilePayRef,
            note = dto.Note,
            status = "pending",
            requested_at = DateTime.UtcNow,
            created_at = DateTime.UtcNow
        };
        _db.transactions.Add(tx);
        await _db.SaveChangesAsync();
        return Ok(tx);
    }

    [HttpPost("decide")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Decide([FromBody] DecideTransactionDto dto)
    {
        var tx = await _db.transactions.FirstOrDefaultAsync(t => t.id == dto.TransactionId && !t.is_deleted);
        if (tx is null) return NotFound();

        if (dto.Decision is not ("approve" or "reject"))
            return BadRequest("Decision must be 'approve' or 'reject'.");

        tx.status = dto.Decision == "approve" ? "approved" : "rejected";
        tx.decided_at = DateTime.UtcNow;
        tx.updated_at = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok(tx);
    }

    [HttpGet("balance/{playerId:long}")]
    public async Task<IActionResult> Balance(long playerId)
    {
        var approved = await _db.transactions
            .Where(t => t.player_id == playerId && t.status == "approved" && !t.is_deleted)
            .SumAsync(t => (decimal?)t.amount_dkk) ?? 0m;

        var spent = await _db.boards
            .Where(b => b.player_id == playerId && !b.is_deleted)
            .SumAsync(b => (decimal?)b.price_dkk) ?? 0m;

        return Ok(new { balance_dkk = approved - spent });
    }
}
