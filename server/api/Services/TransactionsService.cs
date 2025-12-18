using System;
using System.Linq;
using System.Threading.Tasks;
using api.DTOs.Transactions;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class TransactionsService : ITransactionsService
{
    private readonly ApplicationDbContext _db;
    public TransactionsService(ApplicationDbContext db) => _db = db;

    public async Task<CreateTransactionResponse> CreateAsync(CreateTransactionRequest req)
    {
        if (req.AmountDkk <= 0) throw new ArgumentException("Amount must be positive.");

        var playerExists = await _db.players
            .AnyAsync(p => p.id == req.PlayerId && !p.is_deleted);
        if (!playerExists) throw new InvalidOperationException("Player not found.");

        var tx = new transactions
        {
            player_id    = req.PlayerId,
            amount_dkk   = req.AmountDkk,
            mobilepay_ref= req.MobilePayRef,
            note         = req.Note,
            status       = "pending",
            requested_at = DateTime.UtcNow,
            created_at   = DateTime.UtcNow
        };
        _db.transactions.Add(tx);
        await _db.SaveChangesAsync();

        return new CreateTransactionResponse(
            tx.id, tx.player_id, tx.amount_dkk, tx.status, tx.requested_at);
    }

    public async Task<TransactionDto> DecideAsync(DecideTransactionRequest req)
    {
        var tx = await _db.transactions
            .FirstOrDefaultAsync(t => t.id == req.TransactionId && !t.is_deleted);
        if (tx is null) throw new InvalidOperationException("Transaction not found.");

        if (req.Decision is not ("approve" or "reject"))
            throw new ArgumentException("Decision must be 'approve' or 'reject'.");

        tx.status     = req.Decision == "approve" ? "approved" : "rejected";
        tx.decided_at = DateTime.UtcNow;
        tx.updated_at = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new TransactionDto(
            tx.id, tx.player_id, tx.amount_dkk, tx.mobilepay_ref, tx.note,
            tx.status, tx.requested_at, tx.decided_at);
    }

    public async Task<BalanceResponse> GetBalanceAsync(long playerId)
    {
        var approved = await _db.transactions
            .Where(t => t.player_id == playerId && t.status == "approved" && !t.is_deleted)
            .SumAsync(t => (decimal?)t.amount_dkk) ?? 0m;

        var spent = await _db.boards
            .Where(b => b.player_id == playerId && !b.is_deleted)
            .SumAsync(b => (decimal?)b.price_dkk) ?? 0m;

        return new BalanceResponse(approved - spent);
    }
}
