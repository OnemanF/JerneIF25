using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;
using api.DTOs.Boards;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("boards")]
public class BoardsController : ControllerBase
{
    private readonly ISieveProcessor _sieve;
    private readonly IBoardsService _boards;

    public BoardsController(ISieveProcessor sieve, IBoardsService boards)
    {
        _sieve = sieve;
        _boards = boards;
    }
    
    [HttpPost]
    [Authorize(Roles = "player")]
    public async Task<IActionResult> Create([FromBody] CreateBoardRequest dto)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub) || !long.TryParse(sub, out var playerId))
            return Unauthorized("Kun spillere kan købe kuponer.");

        var result = await _boards.CreateAsync(playerId, dto);
        return Ok(result);
    }
}