using System;

namespace api.DTOs.Transactions;

public sealed record CreateTransactionResponse(
    long Id,
    long PlayerId,
    decimal AmountDkk,
    string Status,
    DateTime RequestedAt
);