using System;

namespace api.DTOs.Transactions;

public sealed record TransactionDto(
    long Id,
    long PlayerId,
    decimal AmountDkk,
    string? MobilePayRef,
    string? Note,
    string Status,           // pending | approved | rejected
    DateTime RequestedAt,
    DateTime? DecidedAt
);