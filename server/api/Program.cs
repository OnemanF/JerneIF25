using System.Text;
using System.Text.Json.Serialization;
using api.Etc;                       // AddMyDbContext, GlobalExceptionHandler, etc.
using api.Models;
using JerneIF25.DataAccess.Entities; // AppOptions
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NSwag;                        // AddOpenApiDocument
using Scalar.AspNetCore;
using Sieve.Models;
using Sieve.Services;

namespace api;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;
        var config = builder.Configuration;

        // ----- Options (AppOptions: Db + JwtSecret) -----
        services.AddOptions<AppOptions>()
            .Bind(config.GetSection("AppOptions"))
            .ValidateDataAnnotations()
            .Validate(o => !string.IsNullOrWhiteSpace(o.JwtSecret) && o.JwtSecret.Length >= 32,
                "AppOptions:JwtSecret must be at least 32 characters.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);

        // ----- EF DbContext (your helper already reads from configuration/env) -----
        services.AddMyDbContext();

        // ----- MVC/JSON -----
        services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            opts.JsonSerializerOptions.MaxDepth = 128;
        });

        // ----- OpenAPI/Swagger (NSwag) -----
        services.AddOpenApiDocument(cfg => { cfg.Title = "Jerne IF API"; });

        // ----- CORS + exception handling -----
        services.AddCors();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // ----- Sieve (v3 beta) minimal wiring -----
        services.Configure<SieveOptions>(o =>
        {
            o.CaseSensitive = false;
            o.DefaultPageSize = 10;
            o.MaxPageSize = 100;
        });
        services.AddScoped<ISieveCustomFilterMethods, SieveNoopFilters>();
        services.AddScoped<ISieveProcessor>(sp =>
            new SieveProcessor(
                sp.GetRequiredService<IOptions<SieveOptions>>(),
                sp.GetRequiredService<ISieveCustomFilterMethods>()
            ));

        // ----- JWT Auth (uses AppOptions:JwtSecret) -----
        var opts = config.GetSection("AppOptions").Get<AppOptions>()!;
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opts.JwtSecret));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey
                };
            });
        services.AddAuthorization();

        // ----- Build + pipeline -----
        var app = builder.Build();

        app.UseExceptionHandler(_ => { });

        app.UseOpenApi(); // /swagger/v1/swagger.json
        app.UseSwaggerUi(); // /swagger

        // Optional Scalar viewer:
        app.MapScalarApiReference(o => o.OpenApiRoutePattern = "/swagger/v1/swagger.json");

        app.UseCors(c => c
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .SetIsOriginAllowed(_ => true));

        app.UseAuthentication(); // <-- must be before UseAuthorization
        app.UseAuthorization();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL");
            var pwd = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");

            if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(pwd))
            {
                if (!db.admins.Any(a => a.Email == email))
                {
                    db.admins.Add(new Admin
                    {
                        Email = email,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(pwd)
                    });
                    db.SaveChanges();
                }
            }

            app.MapControllers();

            app.Run();
        }
    }
}
