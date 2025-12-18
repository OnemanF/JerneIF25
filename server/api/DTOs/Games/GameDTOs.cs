namespace api.DTOs.Games;

public sealed record StartGameRequest(DateOnly? WeekStart);
public sealed record PublishWinningNumbersRequest(long GameId, int[] Numbers);
public sealed record SaveDraftRequest(long GameId, int[] Numbers);
public sealed record UndoRequest(long? ClosedGameId);

public sealed record GameResponse(
    long Id,
    DateOnly WeekStart,
    string Status,
    IReadOnlyList<short>? Winning
);

public sealed record GameListRowDto(
    long Id,
    DateOnly WeekStart,
    string Status,
    IReadOnlyList<short>? Winning,
    decimal RevenueDkk
);

public sealed record BoardRowDto(
    long Id,
    long PlayerId,
    string PlayerName,
    decimal PriceDkk,
    short[] Numbers,
    bool IsWinner
);

public sealed record GameBoardsResponse(
    long GameId,
    int WinnersTotal,
    int BoardsTotal,
    IEnumerable<BoardRowDto> Boards
);

public sealed record GameSummaryDto(long Id, int WinnersTotal, int BoardsTotal);