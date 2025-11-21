// server/api/Etc/ServiceCollectionExtensions.cs
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace api.Etc;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection InjectAppOptions(this IServiceCollection services) => services;

    public static IServiceCollection AddMyDbContext(this IServiceCollection services)
    {
        services.AddDbContext<ApplicationDbContext>((sp, opts) =>
        {
            var cfg = sp.GetRequiredService<IConfiguration>();
            var env = Environment.GetEnvironmentVariable("CONNECTION_STRING");
            var appOptionsDb = cfg["AppOptions:Db"];
            var conn = env ?? appOptionsDb 
                ?? cfg.GetConnectionString("Default") // fallback if you later add it
                ?? throw new InvalidOperationException("Missing DB connection string (ENV CONNECTION_STRING or AppOptions:Db).");

            opts.UseNpgsql(conn);
        });
        return services;
    }
}