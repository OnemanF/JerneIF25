using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Controllers;
using JerneIF25.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace tests;

public sealed class GamesControllerTests
{
    private readonly IServiceScopeFactory _scopes;
    public GamesControllerTests(IServiceScopeFactory scopes) => _scopes = scopes;

    private IServiceScope NewScope() => _scopes.CreateScope();

    private static games NewGame(DateOnly weekStart, string status, List<short>? draft = null) => new games
    {
        week_start   = weekStart,
        status       = status,
        winning_nums = draft,
        created_at   = DateTime.UtcNow
    };

    private static void AddBoard(ApplicationDbContext db, long gameId, long playerId, decimal price, params short[] nums)
    {
        db.boards.Add(new boards {
            game_id = gameId, player_id = playerId, price_dkk = price,
            numbers = nums.ToList(), created_at = DateTime.UtcNow
        });
    }

    private static DateOnly IsoMonday(int year, int week)
    {
        var jan4 = new DateTime(year, 1, 4);
        int day = (int)jan4.DayOfWeek; if (day == 0) day = 7;
        var monday = jan4.AddDays(1 - day);
        return DateOnly.FromDateTime(monday.AddDays(7 * (week - 1)));
    }

    private static async Task TruncateAll(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE boards, games, players, transactions RESTART IDENTITY CASCADE;");
    }

    [Fact(DisplayName = "Publish: happy path - closes current and creates/activates next")]
    public async Task Publish_HappyPath_ClosesAndCreatesNext()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tp  = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var sut = new GamesController(db, tp);

        await TruncateAll(db);

        var week51 = IsoMonday(2025, 51);
        var g = NewGame(week51, "active");
        db.games.Add(g);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var res = await sut.Publish(new GamesController.PublishWinningNumbersDto(g.id, new[] { 9, 10, 11 }));
        Assert.IsType<OkObjectResult>(res);

        var curr = await db.games.AsNoTracking().SingleAsync(x => x.id == g.id, TestContext.Current.CancellationToken);
        Assert.Equal("closed", curr.status);
        Assert.NotNull(curr.winning_nums);

        var nextWs = curr.week_start.AddDays(7);
        var next = await db.games.AsNoTracking().SingleAsync(x => x.week_start == nextWs && !x.is_deleted, TestContext.Current.CancellationToken);
        Assert.Equal("active", next.status);
    }

    [Fact(DisplayName = "Publish: unhappy path - no active game")]
    public async Task Publish_Unhappy_NoActive()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tp  = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var sut = new GamesController(db, tp);

        await TruncateAll(db);

        var week51 = IsoMonday(2025, 51);
        db.games.Add(NewGame(week51, "inactive"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var inactiveId = await db.games.Where(g => g.status == "inactive").Select(g => g.id).FirstAsync(TestContext.Current.CancellationToken);
        var res = await sut.Publish(new GamesController.PublishWinningNumbersDto(inactiveId, new[] { 1, 2, 3 }));
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        Assert.Contains("Aktiv uge ikke fundet", bad.Value?.ToString() ?? "");
    }

    [Fact(DisplayName = "Undo: happy path - reopens last closed and inactivates next if needed")]
    public async Task Undo_Happy()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tp  = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var sut = new GamesController(db, tp);

        await TruncateAll(db);

        var week50 = IsoMonday(2025, 50);
        var week51 = week50.AddDays(7);

        var closed = NewGame(week50, "closed", new List<short> { 2, 3, 4 });
        db.games.Add(closed);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var next = NewGame(week51, "active");
        db.games.Add(next);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var res = await sut.Undo(new GamesController.UndoRequest(closed.id));
        Assert.IsType<OkObjectResult>(res);

        var reopened = await db.games.SingleAsync(x => x.id == closed.id, TestContext.Current.CancellationToken);
        Assert.Equal("active", reopened.status);
        Assert.Null(reopened.winning_nums);
    }

    [Fact(DisplayName = "Undo: unhappy path - fails when next week has purchases")]
    public async Task Undo_Unhappy_NextHasPurchases()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tp  = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        var sut = new GamesController(db, tp);

        await TruncateAll(db);

        var week48 = IsoMonday(2025, 48);
        var week49 = week48.AddDays(7);

        var g48 = NewGame(week48, "closed", new List<short> { 1, 2, 3 });
        db.games.Add(g48);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var g49 = NewGame(week49, "active");
        db.games.Add(g49);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        AddBoard(db, g49.id, p.id, 20m, 5, 6, 7);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var res = await sut.Undo(new GamesController.UndoRequest(g48.id));
        var bad = Assert.IsType<BadRequestObjectResult>(res);
        Assert.Contains("Næste uge har allerede køb", bad.Value?.ToString() ?? "");
    }
}
