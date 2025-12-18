using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.DTOs.Subscriptions;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class SubscriptionsService : ISubscriptionsService
{
    private readonly ApplicationDbContext _db;

    public SubscriptionsService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<SubscriptionDto>> ListAsync(long playerId)
    {
        var rows = await _db.board_subscriptions.AsNoTracking()
            .Where(s => !s.is_deleted && s.player_id == playerId && s.is_active)
            .OrderByDescending(s => s.started_at)
            .Select(s => new SubscriptionDto(
                s.id,
                s.player_id,
                (s.numbers ?? new List<short>()).ToArray(),
                s.remaining_weeks,
                s.is_active,
                s.started_at,
                s.canceled_at
            ))
            .ToListAsync();

        return rows;
    }

    public async Task<CreateSubscriptionResponse> CreateAsync(CreateSubscriptionRequest req)
    {
        EnsureValidNumbers(req.Numbers);
        
        var playerExists = await _db.players
            .AnyAsync(p => p.id == req.PlayerId && !p.is_deleted && p.is_active);
        if (!playerExists) throw new InvalidOperationException("Player not found or inactive.");

        var sub = new board_subscriptions
        {
            player_id       = req.PlayerId,
            numbers         = req.Numbers.Select(n => (short)n).ToList(),
            remaining_weeks = Math.Max(0, Math.Min(req.RemainingWeeks, 52)),
            is_active       = true,
            started_at      = DateTime.UtcNow,
            created_at      = DateTime.UtcNow
        };

        _db.board_subscriptions.Add(sub);
        await _db.SaveChangesAsync();

        return new CreateSubscriptionResponse(
            sub.id,
            sub.player_id,
            (sub.numbers ?? new List<short>()).ToArray(),
            sub.remaining_weeks,
            sub.is_active,
            sub.started_at
        );
    }

    public async Task<SubscriptionDto> CancelAsync(CancelSubscriptionRequest req)
    {
        var s = await _db.board_subscriptions
            .FirstOrDefaultAsync(x => x.id == req.SubscriptionId && !x.is_deleted);

        if (s is null) throw new InvalidOperationException("Subscription not found.");

        s.is_active  = false;
        s.canceled_at = DateTime.UtcNow;
        s.updated_at = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new SubscriptionDto(
            s.id,
            s.player_id,
            (s.numbers ?? new List<short>()).ToArray(),
            s.remaining_weeks,
            s.is_active,
            s.started_at,
            s.canceled_at
        );
    }

    private static void EnsureValidNumbers(int[] nums)
    {
        if (nums is null || nums.Length < 5 || nums.Length > 8)
            throw new ArgumentException("Numbers must be 5–8.");
        if (nums.Any(n => n < 1 || n > 16))
            throw new ArgumentException("Numbers must be in range 1..16.");
        if (nums.Distinct().Count() != nums.Length)
            throw new ArgumentException("Numbers must be unique.");
    }
}
