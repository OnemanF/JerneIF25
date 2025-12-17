using System.Text.Json;
using api.Controllers;
using JerneIF25.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace tests;

public static class TestHelpers
{
    public static GamesController GamesController(IServiceScope scope)
    {
        var db  = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tp  = scope.ServiceProvider.GetRequiredService<TimeProvider>();
        return new GamesController(db, tp);
    }
    
    public static T Remap<T>(object value)
    {
        var json = JsonSerializer.Serialize(value);
        return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        })!;
    }
    
    public static T OkPayload<T>(IActionResult result)
    {
        var ok = Assert.IsType<OkObjectResult>(result);
        return Remap<T>(ok.Value!);
    }
}