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
    [Fact(DisplayName = "Active (service): returns existing or creates for current ISO week")]
public async Task Active_ReturnsOrCreates()
{
    using var s = NewScope();
    var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
    await TruncateAll(db);

    var a1 = await svc.GetActiveAsync();
    Assert.NotNull(a1);
    Assert.Equal("active", a1!.status);

    var a2 = await svc.GetActiveAsync(); 
    Assert.Equal(a1.id, a2!.id);
}

[Fact(DisplayName = "Start (service): inactivates current active and activates given week")]
public async Task Start_Happy()
{
    using var s = NewScope();
    var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
    await TruncateAll(db);
    
    var active = await svc.GetActiveAsync();
    Assert.Equal("active", active!.status);

    var week = new DateOnly(active.week_start.Year, active.week_start.Month, active.week_start.Day).AddDays(7);
    var started = await svc.StartAsync(week);

    Assert.Equal("active", started.status);
    Assert.Equal(week, started.week_start);

    var prev = await db.games.AsNoTracking().SingleAsync(g => g.id == active.id);
    Assert.Equal("inactive", prev.status);
}

[Theory(DisplayName = "Publish (service): invalid numbers rejected")]
[InlineData(new int[] { 1, 2 })]           
[InlineData(new int[] { 1, 2, 17 })]       
[InlineData(new int[] { 2, 2, 3 })]       
public async Task Publish_Unhappy_InvalidNumbers(int[] nums)
{
    using var s = NewScope();
    var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
    await TruncateAll(db);

    var ws = new DateOnly(2025, 12, 15);
    var g = NewGame(ws, "active");
    db.games.Add(g);
    await db.SaveChangesAsync(TestContext.Current.CancellationToken);

    await Assert.ThrowsAsync<ArgumentException>(() => svc.PublishAsync(g.id, nums));
}

[Fact(DisplayName = "Draft (service): unhappy – closed game")]
public async Task Draft_Unhappy_Closed()
{
    using var s = NewScope();
    var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
    await TruncateAll(db);

    var ws = new DateOnly(2025, 12, 15);
    var g = NewGame(ws, "closed", new short[] { 1, 2, 3 });
    db.games.Add(g); await db.SaveChangesAsync();

    await Assert.ThrowsAsync<InvalidOperationException>(() => svc.DraftAsync(g.id, new[] { 4, 5, 6 }));
}

[Fact(DisplayName = "Undo (service): unhappy – next week already has purchases")]
public async Task Undo_Unhappy_NextHasPurchases()
{
    using var s = NewScope();
    var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var svc = s.ServiceProvider.GetRequiredService<IGamesService>();
    await TruncateAll(db);

    var week = new DateOnly(2025, 12, 01);
    var closed = NewGame(week, "closed", new short[] { 1, 2, 3 });
    var next   = NewGame(week.AddDays(7), "active");
    db.games.AddRange(closed, next);
    await db.SaveChangesAsync();
    
    var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
    db.players.Add(p); await db.SaveChangesAsync();
    db.boards.Add(new boards { game_id = next.id, player_id = p.id, numbers = new() { 1, 2, 3, 4, 5 }, price_dkk = 20, created_at = DateTime.UtcNow });
    await db.SaveChangesAsync();

    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UndoAsync(closed.id));
    Assert.Contains("køb", ex.Message, StringComparison.OrdinalIgnoreCase);
}

}
