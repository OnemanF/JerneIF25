namespace api.DTOs.Transactions;

public sealed record CreateTransactionRequest(
    long PlayerId,
    decimal AmountDkk,
    string? MobilePayRef,
    string? Note
);