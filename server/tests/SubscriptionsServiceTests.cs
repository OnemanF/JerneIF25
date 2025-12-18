using System;
using System.Linq;
using System.Threading.Tasks;
using api.DTOs.Subscriptions;
using api.Services;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace tests;

public sealed class SubscriptionsServiceTests
{
    private readonly IServiceScopeFactory _scopes;
    public SubscriptionsServiceTests(IServiceScopeFactory scopes) => _scopes = scopes;
    private IServiceScope NewScope() => _scopes.CreateScope();

    private static async Task TruncateAll(ApplicationDbContext db)
        => await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE boards, board_subscriptions, transactions, games, players, player_credentials, admins RESTART IDENTITY CASCADE;");

    [Fact(DisplayName = "List – returns only active subs for player")]
    public async Task List_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        db.board_subscriptions.Add(new board_subscriptions {
            player_id = p.id, numbers = new() {1,2,3,4,5},
            remaining_weeks = 3, is_active = true,
            started_at = DateTime.UtcNow, created_at = DateTime.UtcNow
        });
        db.board_subscriptions.Add(new board_subscriptions {
            player_id = p.id, numbers = new() {6,7,8,9,10},
            remaining_weeks = 1, is_active = false,
            started_at = DateTime.UtcNow, created_at = DateTime.UtcNow, canceled_at = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var list = await svc.ListAsync(p.id);
        Assert.Single(list);
        Assert.True(list[0].IsActive);
        Assert.Equal(p.id, list[0].PlayerId);
    }

    [Fact(DisplayName = "Create – happy path")]
    public async Task Create_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        var res = await svc.CreateAsync(new CreateSubscriptionRequest(
            p.id, new[] {1,2,3,4,5}, 10));

        Assert.True(res.Id > 0);
        Assert.Equal(p.id, res.PlayerId);
        Assert.Equal(10, res.RemainingWeeks);
        Assert.True(res.IsActive);
        Assert.Equal(new short[]{1,2,3,4,5}, res.Numbers);
    }

    [Theory(DisplayName = "Create – unhappy: invalid numbers")]
    [InlineData(new int[] {1,2,3,4})]            // <5
    [InlineData(new int[] {1,2,3,4,5,6,7,8,9})]  // >8
    [InlineData(new int[] {0,2,3,4,5})]          // out of range
    [InlineData(new int[] {1,1,2,3,4})]          // duplicate
    public async Task Create_Unhappy_InvalidNumbers(int[] nums)
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            svc.CreateAsync(new CreateSubscriptionRequest(p.id, nums, 5)));
    }

    [Fact(DisplayName = "Create – unhappy: player not found/inactive")]
    public async Task Create_Unhappy_PlayerNotFound()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        await TruncateAll(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateSubscriptionRequest(12345, new[] {1,2,3,4,5}, 5)));
    }

    [Fact(DisplayName = "Cancel – happy path")]
    public async Task Cancel_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        var sub = new board_subscriptions {
            player_id = p.id, numbers = new() {1,2,3,4,5},
            remaining_weeks = 2, is_active = true, started_at = DateTime.UtcNow, created_at = DateTime.UtcNow
        };
        db.board_subscriptions.Add(sub); await db.SaveChangesAsync();

        var res = await svc.CancelAsync(new CancelSubscriptionRequest(sub.id));

        Assert.False(res.IsActive);
        Assert.NotNull(res.CanceledAt);
    }

    [Fact(DisplayName = "Cancel – unhappy: not found")]
    public async Task Cancel_Unhappy_NotFound()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<ISubscriptionsService>();
        await TruncateAll(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CancelAsync(new CancelSubscriptionRequest(999)));
    }
}
