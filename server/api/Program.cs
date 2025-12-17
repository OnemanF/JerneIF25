using System.Text;
using System.Text.Json.Serialization;
using api.Etc;
using JerneIF25.DataAccess.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSwag;
using Scalar.AspNetCore;
using Sieve.Models;
using Sieve.Services;

namespace api;

public static class Program
{
    public sealed class AppOptions
    {
        public string? Db { get; set; }
        public string JwtSecret { get; set; } = "changeme-min-32-chars-please-change";
    }

    public static void Main(string[] args)
    {
        var builder  = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        var config   = builder.Configuration;
        
        services.AddOptions<AppOptions>()
            .Bind(config.GetSection("AppOptions"))
            .Validate(o => !string.IsNullOrWhiteSpace(o.JwtSecret) && o.JwtSecret.Length >= 32,
                "AppOptions:JwtSecret must be at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        
        services.AddMyDbContext();
        
        services.AddScoped<api.Services.IGamesService, api.Services.GamesService>();

        services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            opts.JsonSerializerOptions.MaxDepth = 128;
        });

        services.AddOpenApiDocument(cfg => cfg.Title = "Jerne IF API");
        services.AddCors();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Sieve
        services.Configure<SieveOptions>(o =>
        {
            o.CaseSensitive   = false;
            o.DefaultPageSize = 10;
            o.MaxPageSize     = 100;
        });
        services.AddScoped<ISieveCustomFilterMethods, SieveNoopFilters>();
        services.AddScoped<ISieveProcessor>(sp =>
            new SieveProcessor(
                sp.GetRequiredService<IOptions<SieveOptions>>(),
                sp.GetRequiredService<ISieveCustomFilterMethods>()));

        // JWT
        var opts       = config.GetSection("AppOptions").Get<AppOptions>()!;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.JwtSecret));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = false,
                    ValidateAudience         = false,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey         = signingKey
                };
            });
        services.AddAuthorization();

        var app = builder.Build();

        app.UseExceptionHandler(_ => { });
        app.UseOpenApi();
        app.UseSwaggerUi();
        app.MapScalarApiReference(o => o.OpenApiRoutePattern = "/swagger/v1/swagger.json");

        app.UseCors(c => c.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().SetIsOriginAllowed(_ => true));
        app.UseAuthentication();
        app.UseAuthorization();

        using (var scope = app.Services.CreateScope())
        {
            var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            SeedAdmin(db, cfg);
        }

        app.MapControllers();
        app.Run();
    }

    private static void SeedAdmin(ApplicationDbContext db, IConfiguration config)
    {
        var email = config["ADMIN_EMAIL"];
        var pwd   = config["ADMIN_PASSWORD"];

        var env   = config["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var isDev = string.Equals(env, "Development", StringComparison.OrdinalIgnoreCase);

        // Dev fallback only if nothing provided
        if ((string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd)) && isDev)
        {
            email ??= "admin@example.com";
            pwd   ??= "admin12345";
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pwd))
            return; 
        
        var admin = db.admins.SingleOrDefault(a => a.email == email);
        var newHash = BCrypt.Net.BCrypt.HashPassword(pwd);

        if (admin is null)
        {
            db.admins.Add(new admins
            {
                email         = email!,
                password_hash = newHash
            });
            db.SaveChanges();
            return;
        }
        
        if (!BCrypt.Net.BCrypt.Verify(pwd, admin.password_hash))
        {
            admin.password_hash = newHash;
            db.SaveChanges();
        }
    }
}
