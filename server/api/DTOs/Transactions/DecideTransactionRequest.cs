namespace api.DTOs.Transactions;

public sealed record DecideTransactionRequest(
    long TransactionId,
    string Decision  
);