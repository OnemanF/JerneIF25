using System;
using System.Threading.Tasks;
using api.DTOs.Auth;
using api.Services;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace tests;

public sealed class AuthServiceTests
{
    private readonly IServiceScopeFactory _scopes;
    public AuthServiceTests(IServiceScopeFactory scopes) => _scopes = scopes;
    private IServiceScope NewScope() => _scopes.CreateScope();

    private static async Task TruncateAll(ApplicationDbContext db)
        => await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE boards, board_subscriptions, transactions, games, players, player_credentials, admins RESTART IDENTITY CASCADE;");

    [Fact(DisplayName = "Admin login – happy")]
    public async Task AdminLogin_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IAuthService>();
        await TruncateAll(db);
        
        db.admins.Add(new admins
        {
            email = "admin@example.com",
            password_hash = BCrypt.Net.BCrypt.HashPassword("secret")
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var res = await svc.AdminLoginAsync(new AdminLoginRequest("admin@example.com", "secret"));
        Assert.False(string.IsNullOrWhiteSpace(res.Token));
    }

    [Fact(DisplayName = "Admin login – unhappy: bad credentials")]
    public async Task AdminLogin_Unhappy_BadCreds()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IAuthService>();
        await TruncateAll(db);

        db.admins.Add(new admins
        {
            email = "admin@example.com",
            password_hash = BCrypt.Net.BCrypt.HashPassword("secret")
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AdminLoginAsync(new AdminLoginRequest("admin@example.com", "wrong")));
    }

    [Fact(DisplayName = "Player register – happy")]
    public async Task PlayerRegister_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IAuthService>();
        await TruncateAll(db);

        var res = await svc.PlayerRegisterAsync(new PlayerRegisterRequest("P", "p@x.dk", "pw12345", null));
        Assert.False(string.IsNullOrWhiteSpace(res.Token));

        var p = await db.players.AsNoTracking().SingleAsync();
        var c = await db.player_credentials.AsNoTracking().SingleAsync();
        Assert.Equal(p.id, c.player_id);
        Assert.Equal("p@x.dk", c.email);
    }

    [Fact(DisplayName = "Player register – unhappy: email exists")]
    public async Task PlayerRegister_Unhappy_EmailExists()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IAuthService>();
        await TruncateAll(db);

        // seed existing cred
        db.players.Add(new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var p = await db.players.AsNoTracking().SingleAsync();

        db.player_credentials.Add(new player_credentials
        {
            player_id = p.id,
            email = "p@x.dk",
            password_hash = BCrypt.Net.BCrypt.HashPassword("x"),
            created_at = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PlayerRegisterAsync(new PlayerRegisterRequest("Q", "p@x.dk", "z", null)));
    }

    [Fact(DisplayName = "Player login – happy")]
    public async Task PlayerLogin_Happy()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IAuthService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        db.player_credentials.Add(new player_credentials
        {
            player_id = p.id,
            email = "p@x.dk",
            password_hash = BCrypt.Net.BCrypt.HashPassword("pw12345"),
            created_at = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var res = await svc.PlayerLoginAsync(new PlayerLoginRequest("p@x.dk", "pw12345"));
        Assert.False(string.IsNullOrWhiteSpace(res.Token));
    }

    [Fact(DisplayName = "Player login – unhappy: wrong password")]
    public async Task PlayerLogin_Unhappy_BadPassword()
    {
        using var s = NewScope();
        var db  = s.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var svc = s.ServiceProvider.GetRequiredService<IAuthService>();
        await TruncateAll(db);

        var p = new players { name = "P", email = "p@x.dk", is_active = true, created_at = DateTime.UtcNow };
        db.players.Add(p); await db.SaveChangesAsync();

        db.player_credentials.Add(new player_credentials
        {
            player_id = p.id,
            email = "p@x.dk",
            password_hash = BCrypt.Net.BCrypt.HashPassword("pw12345"),
            created_at = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.PlayerLoginAsync(new PlayerLoginRequest("p@x.dk", "WRONG")));
    }
}
