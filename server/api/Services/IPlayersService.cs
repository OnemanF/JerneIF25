using api.DTOs.Players;

namespace api.Services;

public interface IPlayersService
{
    Task<PlayerResponse> CreateAsync(CreatePlayerRequest dto, CancellationToken ct = default);
    Task<PlayerResponse> UpdateAsync(long id, UpdatePlayerRequest dto, CancellationToken ct = default);
    Task SoftDeleteAsync(long id, CancellationToken ct = default);
}