using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace tests;

public sealed class TestDataFactory
{
    private readonly ApplicationDbContext _db;

    public TestDataFactory(ApplicationDbContext db) => _db = db;

    public async Task ResetAsync()
    {
        _db.RemoveRange(_db.boards);
        _db.RemoveRange(_db.players);
        _db.RemoveRange(_db.games);
        await _db.SaveChangesAsync();
    }

    public async Task<games> AddGameAsync(DateOnly ws, string status, IEnumerable<short>? win = null, bool deleted = false)
    {
        var g = new games
        {
            week_start = ws,
            status = status,
            winning_nums = win?.ToList(),
            created_at = DateTime.UtcNow,
            is_deleted = deleted
        };
        _db.games.Add(g);
        await _db.SaveChangesAsync();
        return g;
    }

    public async Task<players> AddPlayerAsync(string name = "P", string? email = null)
    {
        var p = new players { name = name, email = email, is_active = true, is_deleted = false, created_at = DateTime.UtcNow };
        _db.players.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }

    public async Task<boards> AddBoardAsync(long gameId, long playerId, decimal price, params short[] nums)
    {
        var b = new boards
        {
            game_id = gameId,
            player_id = playerId,
            price_dkk = price,
            numbers = nums.ToList(),
            is_deleted = false,
            created_at = DateTime.UtcNow
        };
        _db.boards.Add(b);
        await _db.SaveChangesAsync();
        return b;
    }
}   