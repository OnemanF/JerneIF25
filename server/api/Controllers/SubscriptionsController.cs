using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using api.DTOs.Subscriptions;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("subscriptions")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionsService _svc;
    public SubscriptionsController(ISubscriptionsService svc) => _svc = svc;

    [HttpGet("{playerId:long}")]
    public async Task<IActionResult> List(long playerId)
    {
        var rows = await _svc.ListAsync(playerId);
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest dto)
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

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelSubscriptionRequest dto)
    {
        try
        {
            var res = await _svc.CancelAsync(dto);
            return Ok(res);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}