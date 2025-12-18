using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class GamesService : IGamesService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _tp;
    private static DateOnly NextWeek(DateOnly ws) => ws.AddDays(7);

    public GamesService(ApplicationDbContext db, TimeProvider tp)
    {
        _db = db;
        _tp = tp;
    }

    public async Task<IReadOnlyList<games>> ListAsync(string? status)
    {
        var q = _db.games.AsNoTracking().Where(g => !g.is_deleted);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToLowerInvariant();
            if (s is "active" or "closed" or "inactive")
                q = q.Where(g => g.status == s);
        }
        else
        {
            q = q.Where(g => g.status == "active" || g.status == "closed");
        }

        return await q.OrderByDescending(g => g.week_start).ToListAsync();
    }

    public async Task<games?> GetActiveAsync()
    {
        var active = await _db.games.AsNoTracking()
            .FirstOrDefaultAsync(g => g.status == "active" && !g.is_deleted);

        if (active is not null) return active;

        var ws = Etc.TimeAndCalendar.IsoWeekStartLocal(_tp.GetUtcNow());
        var existing = await _db.games.FirstOrDefaultAsync(g => g.week_start == ws && !g.is_deleted);

        if (existing is null)
        {
            var ng = new games { week_start = ws, status = "active", created_at = DateTime.UtcNow };
            _db.games.Add(ng);
            await _db.SaveChangesAsync();
            return ng;
        }

        if (existing.status != "active")
        {
            existing.status = "active";
            existing.updated_at = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        return existing;
    }

    public async Task<games> StartAsync(DateOnly? weekStart)
    {
        var active = await _db.games.FirstOrDefaultAsync(g => g.status == "active" && !g.is_deleted);
        if (active is not null)
        {
            active.status = "inactive";
            active.updated_at = DateTime.UtcNow;
        }

        var ws = weekStart ?? Etc.TimeAndCalendar.IsoWeekStartLocal(_tp.GetUtcNow());
        var g = await _db.games.FirstOrDefaultAsync(x => x.week_start == ws && !x.is_deleted);

        if (g is null)
        {
            g = new games { week_start = ws, status = "active", created_at = DateTime.UtcNow };
            _db.games.Add(g);
        }
        else
        {
            g.status = "active";
            g.updated_at = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return g;
    }

    public async Task<(games Closed, games Next)> PublishAsync(long gameId, int[] numbers)
    {
        if (numbers is null || numbers.Length != 3) throw new ArgumentException("Præcis 3 tal kræves.");
        if (numbers.Any(n => n < 1 || n > 16)) throw new ArgumentException("Tal skal være 1..16.");
        if (numbers.Distinct().Count() != 3) throw new ArgumentException("Tal skal være unikke.");

        var g = await _db.games.FirstOrDefaultAsync(x => x.id == gameId && x.status == "active" && !x.is_deleted);
        if (g is null) throw new InvalidOperationException("Aktiv uge ikke fundet.");

        g.winning_nums = numbers.Select(n => (short)n).ToList();
        g.status       = "closed";
        g.published_at = DateTime.UtcNow;
        g.updated_at   = DateTime.UtcNow;
        
        var nextWs = NextWeek(g.week_start);
        var nextAny = await _db.games.FirstOrDefaultAsync(x => x.week_start == nextWs);

        games next;
        if (nextAny is null)
        {
            next = new games { week_start = nextWs, status = "active", created_at = DateTime.UtcNow, is_deleted = false };
            _db.games.Add(next);
        }
        else
        {
            next = nextAny;
            next.is_deleted = false;
            next.status     = "active";
            next.updated_at = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (g, next);
    }

    public async Task<games> DraftAsync(long gameId, int[] numbers)
    {
        if (numbers is null || numbers.Length != 3) throw new ArgumentException("Præcis 3 tal kræves.");
        if (numbers.Any(n => n < 1 || n > 16)) throw new ArgumentException("Tal skal være 1..16.");
        if (numbers.Distinct().Count() != 3) throw new ArgumentException("Tal skal være unikke.");

        var g = await _db.games.FirstOrDefaultAsync(x => x.id == gameId && !x.is_deleted);
        if (g is null) throw new InvalidOperationException("Uge ikke fundet.");
        if (g.status == "closed") throw new InvalidOperationException("Kan ikke gemme på lukket uge.");
        if (g.status != "active") throw new InvalidOperationException("Kun aktiv uge kan få udkast.");

        g.winning_nums = numbers.Select(n => (short)n).ToList();
        g.updated_at   = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return g;
    }

    public async Task<games> UndoAsync(long? closedGameId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var closed = closedGameId.HasValue
            ? await _db.games.FirstOrDefaultAsync(g => g.id == closedGameId && !g.is_deleted && g.status == "closed")
            : await _db.games.Where(g => !g.is_deleted && g.status == "closed")
                             .OrderByDescending(g => g.week_start)
                             .FirstOrDefaultAsync();

        if (closed is null) throw new InvalidOperationException("Ingen lukket uge at fortryde.");

        var nextWs = NextWeek(closed.week_start);
        var next = await _db.games.FirstOrDefaultAsync(g => g.week_start == nextWs);

        if (next is not null && await _db.boards.AnyAsync(b => !b.is_deleted && b.game_id == next.id))
            throw new InvalidOperationException("Næste uge har allerede køb – fortryd ikke muligt.");

        var currentActive = await _db.games.FirstOrDefaultAsync(g => g.status == "active" && !g.is_deleted);

        if (currentActive is not null)
        {
            if (next is not null && currentActive.id == next.id)
            {
                next.is_deleted = false;
                next.status     = "inactive";
                next.updated_at = DateTime.UtcNow;
            }
            else
            {
                currentActive.status   = "inactive";
                currentActive.updated_at = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync();
        }

        closed.status       = "active";
        closed.published_at = null;
        closed.winning_nums = null;
        closed.updated_at   = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return closed;
    }
}
