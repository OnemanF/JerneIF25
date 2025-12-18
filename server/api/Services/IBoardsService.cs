using System.Threading.Tasks;
using api.DTOs.Boards;

namespace api.Services;

public interface IBoardsService
{
    Task<BoardResponse> CreateAsync(long playerId, CreateBoardRequest req);
}