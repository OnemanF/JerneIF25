namespace api.DTOs.Boards;

public sealed record CreateBoardRequest(long GameId, int[] Numbers, int RepeatGames);