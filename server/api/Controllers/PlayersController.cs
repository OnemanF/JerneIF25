using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using JerneIF25.DataAccess.Entities;

[ApiController]
[Route("players")]
public class PlayersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISieveProcessor _sieve;

    public PlayersController(ApplicationDbContext db, ISieveProcessor sieve)
    {
        _db = db;
        _sieve = sieve;
    }

    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieveModel)
    {
        var query = _db.players.AsNoTracking();
        var filtered = _sieve.Apply(sieveModel, query); // supports ?sorts=created_at&filters=name@=john
        var results = await filtered.ToListAsync();
        return Ok(results);
    }
}