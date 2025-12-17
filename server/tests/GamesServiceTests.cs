using System;
using System.Linq;
using System.Threading.Tasks;
using api.Services;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace tests;

public sealed class GamesServiceTests
{
    private readonly IServiceScopeFactory _scopes;
    public GamesServiceTests(IServiceScopeFactory scopes) => _scopes = scopes;
    private IServiceScope NewScope() => _scopes.CreateScope();

    private static games NewGame(DateOnly ws, string status, short[]? draft = null) => new games
    {
        week_start   = ws,
        status       = status,
        winning_nums = draft?.ToList(),
        created_at   = DateTime.UtcNow
    };

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

    [Fact(DisplayName = "Publish (service): closes current & creates/activates next")]
    public async Task Publish_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
        await TruncateAll(db);

        var week51 = IsoMonday(2025, 51);
        var g = NewGame(week51, "active");
        db.games.Add(g); await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var (closed, next) = await svc.PublishAsync(g.id, new[] { 9, 10, 11 });
        Assert.Equal("closed", closed.status);
        Assert.Equal("active", next.status);

        var nextWs = closed.week_start.AddDays(7);
        Assert.Equal(nextWs, next.week_start);
    }

    [Fact(DisplayName = "Publish (service): unhappy – no active game")]
    public async Task Publish_Unhappy_NoActive()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
        await TruncateAll(db);

        var week51 = IsoMonday(2025, 51);
        db.games.Add(NewGame(week51, "inactive"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var id = await db.games.Where(x => x.status == "inactive").Select(x => x.id).FirstAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.PublishAsync(id, new[] { 1, 2, 3 }));
    }

    [Fact(DisplayName = "Undo (service): happy – reopens last closed")]
    public async Task Undo_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
        await TruncateAll(db);

        var week50 = IsoMonday(2025, 50);
        var week51 = week50.AddDays(7);
        db.games.Add(NewGame(week50, "closed", new short[] { 2, 3, 4 }));
        db.games.Add(NewGame(week51, "active"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var reopened = await svc.UndoAsync(null); // last closed
        Assert.Equal("active", reopened.status);
        Assert.Null(reopened.winning_nums);
    }

    [Fact(DisplayName = "Draft (service): unhappy – non-active cannot draft")]
    public async Task Draft_Unhappy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
        await TruncateAll(db);

        var week = IsoMonday(2025, 49);
        var g = NewGame(week, "inactive");
        db.games.Add(g); await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DraftAsync(g.id, new[] { 1, 2, 3 }));
    }
}
