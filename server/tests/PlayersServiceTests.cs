using System.Threading.Tasks;
using api.DTOs.Players;
using api.Services;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace tests;

public sealed class PlayersServiceTests
{
    private readonly IServiceScopeFactory _scopes;
    public PlayersServiceTests(IServiceScopeFactory scopes) => _scopes = scopes;
    private IServiceScope NewScope() => _scopes.CreateScope();

    private static async Task ResetAsync(ApplicationDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE players RESTART IDENTITY CASCADE;");
    }

    [Fact(DisplayName = "Create player – happy path")]
    public async Task Create_Happy()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IPlayersService>();

        await ResetAsync(db);

        var dto = new CreatePlayerRequest { Name = "Alice", Email = "alice@example.com", Phone = "12345678", IsActive = true };
        var res = await svc.CreateAsync(dto, TestContext.Current.CancellationToken);

        Assert.True(res.Id > 0);
        Assert.Equal("Alice", res.Name);
        Assert.True(res.IsActive);
    }

    [Fact(DisplayName = "Create player – unhappy: invalid name")]
    public async Task Create_Unhappy_InvalidName()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IPlayersService>();

        await ResetAsync(db);

        await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await svc.CreateAsync(new CreatePlayerRequest { Name = " ", Email = "x@y.z" }, TestContext.Current.CancellationToken);
        });
    }

    [Fact(DisplayName = "Update player – happy path")]
    public async Task Update_Happy()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IPlayersService>();

        await ResetAsync(db);

        var p = await svc.CreateAsync(new CreatePlayerRequest { Name = "Bob" }, TestContext.Current.CancellationToken);

        var updated = await svc.UpdateAsync(p.Id, new UpdatePlayerRequest
        {
            Name = "Bobby",
            Phone = "22334455",
            Email = "bobby@example.com",
            IsActive = true,
            MemberExpiresAt = null
        }, TestContext.Current.CancellationToken);

        Assert.Equal("Bobby", updated.Name);
        Assert.True(updated.IsActive);
    }

    [Fact(DisplayName = "Update player – unhappy: not found")]
    public async Task Update_Unhappy_NotFound()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IPlayersService>();

        await ResetAsync(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await svc.UpdateAsync(9999, new UpdatePlayerRequest { Name = "X", IsActive = true }, TestContext.Current.CancellationToken);
        });
    }

    [Fact(DisplayName = "Soft delete – happy path")]
    public async Task Delete_Happy()
    {
        using var scope = NewScope();
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = scope.ServiceProvider.GetRequiredService<IPlayersService>();

        await ResetAsync(db);

        var p = await svc.CreateAsync(new CreatePlayerRequest { Name = "ToDelete" }, TestContext.Current.CancellationToken);
        await svc.SoftDeleteAsync(p.Id, TestContext.Current.CancellationToken);

        var exists = await db.players.AnyAsync(x => x.id == p.Id && !x.is_deleted, TestContext.Current.CancellationToken);
        Assert.False(exists);
    }

    [Fact(DisplayName = "Soft delete – unhappy: not found")]
    public async Task Delete_Unhappy_NotFound()
    {
        using var scope = NewScope();
        var svc = scope.ServiceProvider.GetRequiredService<IPlayersService>();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.SoftDeleteAsync(424242, TestContext.Current.CancellationToken));
    }
}
