using System.Text.Json.Serialization;
using api.Etc;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;
using Scalar.AspNetCore;

namespace api;

public class Program
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        // Your own helpers
        services.InjectAppOptions();
        services.AddMyDbContext();

        // MVC + JSON
        services.AddControllers().AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            opts.JsonSerializerOptions.MaxDepth = 128;
        });

        // OpenAPI/Swagger (NSwag)
        services.AddOpenApiDocument(); // <-- remove AddStringConstants; you don’t have that extension

        // CORS / exception handling
        services.AddCors();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // --- Sieve (v3 beta) minimal registration ---
        services.AddOptions<SieveOptions>().Configure(o =>
        {
            o.CaseSensitive = false;
            o.DefaultPageSize = 10;
            o.MaxPageSize = 100;
        });
        services.AddScoped<ISieveCustomFilterMethods, SieveNoopFilters>(); // only filters
        services.AddScoped<ISieveProcessor, SieveProcessor>();             // use built-in processor
        // ------------------------------------------------
    }

    public static void Main()
    {
        var builder = WebApplication.CreateBuilder();
        ConfigureServices(builder.Services);

        var app = builder.Build();

        app.UseExceptionHandler(_ => { });

        // Swagger/NSwag UI (must be AFTER build, not inside ConfigureServices)
        app.UseOpenApi();
        app.UseSwaggerUi();

        // Scalar optional (if you use it)
        app.MapScalarApiReference(o => o.OpenApiRoutePattern = "/swagger/v1/swagger.json");

        app.UseCors(c => c
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .SetIsOriginAllowed(_ => true));

        app.MapControllers();

        // If you don’t have a real implementation yet, stub ISeeder or remove this block
        // using (var scope = app.Services.CreateScope())
        //     await scope.ServiceProvider.GetRequiredService<ISeeder>().Seed();

        app.Run();
    }
}
