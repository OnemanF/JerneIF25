namespace api.DTOs.Boards;

public sealed record BoardResponse(
    long Id,
    long GameId,
    long PlayerId,
    decimal PriceDkk,
    short[] Numbers,
    DateTime? PurchasedAt
);