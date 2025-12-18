using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using JerneIF25.DataAccess.Entities;
using api.DTOs.Transactions;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("transactions")]
public class TransactionsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ISieveProcessor _sieve;
    private readonly ITransactionsService _svc;

    public TransactionsController(ApplicationDbContext db, ISieveProcessor sieve, ITransactionsService svc)
    {
        _db = db;
        _sieve = sieve;
        _svc = svc;
    }
    
    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Get([FromQuery] SieveModel sieve)
    {
        var q = _db.transactions.AsNoTracking().Where(t => !t.is_deleted);
        q = _sieve.Apply(sieve, q);
        return Ok(await q.ToListAsync());
    }

    [HttpPost]
    [Authorize] 
    public async Task<IActionResult> Create([FromBody] CreateTransactionRequest dto)
    {
        try
        {
            var res = await _svc.CreateAsync(dto);
            return Ok(res);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("decide")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Decide([FromBody] DecideTransactionRequest dto)
    {
        try
        {
            var res = await _svc.DecideAsync(dto);
            return Ok(res);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpGet("balance/{playerId:long}")]
    [Authorize] 
    public async Task<IActionResult> Balance(long playerId)
    {
        var res = await _svc.GetBalanceAsync(playerId);
        return Ok(res);
    }
}
