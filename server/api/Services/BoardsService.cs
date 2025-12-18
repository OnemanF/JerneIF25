using System.Linq;
using System.Threading.Tasks;
using api.DTOs.Boards;
using api.Etc;
using JerneIF25.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class BoardsService : IBoardsService
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _tp;

    public BoardsService(ApplicationDbContext db, TimeProvider tp)
    {
        _db = db;
        _tp = tp;
    }

    public async Task<BoardResponse> CreateAsync(long playerId, CreateBoardRequest req)
    {
        BoardPricing.EnsureValidNumbers(req.Numbers);
        
        var player = await _db.players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.id == playerId && !p.is_deleted);
        if (player is null) throw new InvalidOperationException("Player not found.");
        if (!player.is_active) throw new InvalidOperationException("Player is inactive.");
        
        var game = await _db.games
            .FirstOrDefaultAsync(g => g.id == req.GameId && g.status == "active" && !g.is_deleted);
        if (game is null) throw new InvalidOperationException("Active game not found.");
        
        if (TimeAndCalendar.CutoffPassed(_tp, game.week_start))
            throw new InvalidOperationException("Køb lukket for denne uge (efter lørdag kl. 17).");
        
        var price = BoardPricing.PriceForCount(req.Numbers.Length);

        var entity = new boards
        {
            game_id      = req.GameId,
            player_id    = playerId,
            numbers      = req.Numbers.Select(n => (short)n).ToList(),
            price_dkk    = price,
            purchased_at = DateTime.UtcNow,
            created_at   = DateTime.UtcNow
        };
        _db.boards.Add(entity);
        
        var repeat = Math.Max(0, Math.Min(req.RepeatGames, 52));
        if (repeat > 0)
        {
            _db.board_subscriptions.Add(new board_subscriptions
            {
                player_id       = playerId,
                numbers         = req.Numbers.Select(n => (short)n).ToList(),
                remaining_weeks = repeat,
                is_active       = true,
                started_at      = DateTime.UtcNow,
                created_at      = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return new BoardResponse(
            entity.id,
            entity.game_id,
            entity.player_id,
            entity.price_dkk,
            (entity.numbers ?? new List<short>()).ToArray(),
            entity.purchased_at
        );
    }
}
