using JerneIF25.DataAccess.Entities;

namespace api.Services;

public interface IGamesService
{
    Task<IReadOnlyList<games>> ListAsync(string? status);
    Task<games?> GetActiveAsync();
    Task<games> StartAsync(DateOnly? weekStart);
    Task<(games Closed, games Next)> PublishAsync(long gameId, int[] numbers);
    Task<games> DraftAsync(long gameId, int[] numbers);
    Task<games> UndoAsync(long? closedGameId);
}