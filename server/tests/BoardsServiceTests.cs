using System;
using System.Linq;
using System.Threading.Tasks;
using api.DTOs.Boards;
using api.Services;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace tests;

public sealed class BoardsServiceTests
{
    private readonly IServiceScopeFactory _scopes;
    public BoardsServiceTests(IServiceScopeFactory scopes) => _scopes = scopes;
    private IServiceScope NewScope() => _scopes.CreateScope();

    private static async Task TruncateAll(ApplicationDbContext db)
        => await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE boards, board_subscriptions, games, players, transactions RESTART IDENTITY CASCADE;");

    private static players NewPlayer(string email = "p@x.dk", bool active = true) => new()
    {
        name = "P",
        email = email,
        is_active = active,
        is_deleted = false,
        created_at = DateTime.UtcNow
    };

    private static games NewGame(DateOnly weekStart, string status) => new()
    {
        week_start = weekStart,
        status = status,
        created_at = DateTime.UtcNow,
        is_deleted = false
    };

    private static DateOnly IsoMonday(int year, int week)
    {
        var jan4 = new DateTime(year, 1, 4);
        int day = (int)jan4.DayOfWeek; if (day == 0) day = 7;
        var monday = jan4.AddDays(1 - day);
        return DateOnly.FromDateTime(monday.AddDays(7 * (week - 1)));
    }

    [Fact(DisplayName = "Create board – happy path")]
    public async Task Create_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IBoardsService>();
        await TruncateAll(db);

        var p = NewPlayer();
        db.players.Add(p); await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        
        var week51 = IsoMonday(2025, 51);
        var g = NewGame(week51, "active");
        db.games.Add(g); await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var req = new CreateBoardRequest(g.id, new[] { 1, 3, 5, 7, 9 }, 4);
        var resp = await svc.CreateAsync(p.id, req);

        Assert.True(resp.Id > 0);
        Assert.Equal(g.id, resp.GameId);
        Assert.Equal(p.id, resp.PlayerId);
        Assert.Equal(5, resp.Numbers.Length);
        Assert.NotNull(resp.PurchasedAt);
        
        var sub = await db.board_subscriptions.AsNoTracking().SingleOrDefaultAsync();
        Assert.NotNull(sub);
        Assert.Equal(4, sub!.remaining_weeks);
        Assert.True(sub.is_active);
    }

    [Fact(DisplayName = "Create board – unhappy: invalid numbers")]
    public async Task Create_Unhappy_InvalidNumbers()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IBoardsService>();
        await TruncateAll(db);

        var p = NewPlayer();
        db.players.Add(p); await db.SaveChangesAsync();

        var week51 = IsoMonday(2025, 51);
        var g = NewGame(week51, "active");
        db.games.Add(g); await db.SaveChangesAsync();
        
        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(p.id, new CreateBoardRequest(g.id, new[] { 1, 2, 3, 4 }, 0)));
        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(p.id, new CreateBoardRequest(g.id, new[] { 2, 2, 3, 4, 5 }, 0)));
        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateAsync(p.id, new CreateBoardRequest(g.id, new[] { 1, 2, 3, 4, 99 }, 0)));
    }

    [Fact(DisplayName = "Create board – unhappy: player inactive")]
    public async Task Create_Unhappy_PlayerInactive()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IBoardsService>();
        await TruncateAll(db);

        var p = NewPlayer(active: false);
        db.players.Add(p); await db.SaveChangesAsync();

        var week51 = IsoMonday(2025, 51);
        var g = NewGame(week51, "active");
        db.games.Add(g); await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(p.id, new CreateBoardRequest(g.id, new[] { 1, 2, 3, 4, 5 }, 0)));
    }

    [Fact(DisplayName = "Create board – unhappy: no active game")]
    public async Task Create_Unhappy_NoActiveGame()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IBoardsService>();
        await TruncateAll(db);

        var p = NewPlayer();
        db.players.Add(p); await db.SaveChangesAsync();
        
        var week51 = IsoMonday(2025, 51);
        var g = NewGame(week51, "inactive");
        db.games.Add(g); await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(p.id, new CreateBoardRequest(g.id, new[] { 1, 2, 3, 4, 5 }, 0)));
    }

    [Fact(DisplayName = "Create board – unhappy: cutoff passed")]
    public async Task Create_Unhappy_Cutoff()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IBoardsService>();
        await TruncateAll(db);

        var p = NewPlayer();
        db.players.Add(p); await db.SaveChangesAsync();
        
        var prevWeek = IsoMonday(2025, 50);
        var g = NewGame(prevWeek, "active");
        db.games.Add(g); await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(p.id, new CreateBoardRequest(g.id, new[] { 1, 2, 3, 4, 5 }, 0)));
        Assert.Contains("Køb lukket", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
