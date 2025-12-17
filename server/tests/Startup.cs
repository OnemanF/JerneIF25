using System;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using Testcontainers.PostgreSql;
using api.Services; 

namespace tests;

public sealed class Startup
{
    private static PostgreSqlContainer? _pg;
    private static bool _started;

    public void ConfigureServices(IServiceCollection services)
    {
        _pg ??= new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithDatabase("testdb")
            .WithCleanUp(true)
            .Build();

        if (!_started)
        {
            _pg.StartAsync().GetAwaiter().GetResult();
            _started = true;
        }

        var cs = _pg.GetConnectionString();
        services.AddDbContext<ApplicationDbContext>(o => o.UseNpgsql(cs));

        using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(cs).Options);
        db.Database.EnsureCreated();

        services.AddSingleton<TimeProvider>(new FakeTimeProvider(
            new DateTimeOffset(2025, 12, 15, 9, 0, 0, TimeSpan.Zero)));

        services.AddScoped<TestDataFactory>();
        
        services.AddScoped<IGamesService, GamesService>();
    }
}