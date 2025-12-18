using System;
using System.Threading.Tasks;
using api.DTOs.Transactions;
using api.Services;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace tests;

public sealed class TransactionsServiceTests
{
    private readonly IServiceScopeFactory _scopes;
    public TransactionsServiceTests(IServiceScopeFactory scopes) => _scopes = scopes;
    private IServiceScope NewScope() => _scopes.CreateScope();

    private static async Task TruncateAll(ApplicationDbContext db)
        => await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE boards, board_subscriptions, transactions, games, players, player_credentials, admins RESTART IDENTITY CASCADE;");

    [Fact(DisplayName = "Create – happy path")]
    public async Task Create_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ITransactionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        var res = await svc.CreateAsync(new CreateTransactionRequest(p.id, 200m, "mp-123", "deposit"));
        Assert.True(res.Id > 0);
        Assert.Equal(p.id, res.PlayerId);
        Assert.Equal(200m, res.AmountDkk);
        Assert.Equal("pending", res.Status);
    }

    [Fact(DisplayName = "Create – unhappy: negative amount")]
    public async Task Create_Unhappy_Negative()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ITransactionsService>();
        await TruncateAll(db);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(new CreateTransactionRequest(1, -5m, null, null)));
    }

    [Fact(DisplayName = "Create – unhappy: player not found")]
    public async Task Create_Unhappy_PlayerNotFound()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ITransactionsService>();
        await TruncateAll(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateTransactionRequest(999, 50m, null, null)));
    }

    [Fact(DisplayName = "Decide – happy: approve and reject")]
    public async Task Decide_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ITransactionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        var created = await svc.CreateAsync(new CreateTransactionRequest(p.id, 100m, null, null));
        var approved = await svc.DecideAsync(new DecideTransactionRequest(created.Id, "approve"));
        Assert.Equal("approved", approved.Status);
        Assert.NotNull(approved.DecidedAt);

        var created2 = await svc.CreateAsync(new CreateTransactionRequest(p.id, 30m, null, null));
        var rejected = await svc.DecideAsync(new DecideTransactionRequest(created2.Id, "reject"));
        Assert.Equal("rejected", rejected.Status);
    }

    [Fact(DisplayName = "Decide – unhappy: invalid decision")]
    public async Task Decide_Unhappy_InvalidDecision()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ITransactionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        var created = await svc.CreateAsync(new CreateTransactionRequest(p.id, 10m, null, null));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.DecideAsync(new DecideTransactionRequest(created.Id, "maybe")));
    }

    [Fact(DisplayName = "Decide – unhappy: not found")]
    public async Task Decide_Unhappy_NotFound()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ITransactionsService>();
        await TruncateAll(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.DecideAsync(new DecideTransactionRequest(12345, "approve")));
    }

    [Fact(DisplayName = "Balance – computes approved minus spent")]
    public async Task Balance_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ITransactionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); 
        await db.SaveChangesAsync();
        
        var g = new games
        {
            week_start = DateOnly.FromDateTime(DateTime.UtcNow.Date), 
            status     = "active",
            created_at = DateTime.UtcNow
        };
        db.games.Add(g);
        await db.SaveChangesAsync();

        // approved + rejected + pending
        db.transactions.Add(new transactions { player_id = p.id, amount_dkk = 200, status = "approved", requested_at = DateTime.UtcNow, decided_at = DateTime.UtcNow, created_at = DateTime.UtcNow });
        db.transactions.Add(new transactions { player_id = p.id, amount_dkk = 50,  status = "rejected", requested_at = DateTime.UtcNow, decided_at = DateTime.UtcNow, created_at = DateTime.UtcNow });
        db.transactions.Add(new transactions { player_id = p.id, amount_dkk = 75,  status = "pending",  requested_at = DateTime.UtcNow, created_at = DateTime.UtcNow });
        await db.SaveChangesAsync();
        
        db.boards.Add(new boards { player_id = p.id, game_id = g.id, numbers = new(){1,2,3,4,5}, price_dkk = 60m, created_at = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var bal = await svc.GetBalanceAsync(p.id);
        Assert.Equal(140m, bal.BalanceDkk); 
    }

    
}
