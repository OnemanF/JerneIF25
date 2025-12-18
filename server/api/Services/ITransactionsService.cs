using System.Threading.Tasks;
using api.DTOs.Transactions;

namespace api.Services;

public interface ITransactionsService
{
    Task<CreateTransactionResponse> CreateAsync(CreateTransactionRequest req);
    Task<TransactionDto> DecideAsync(DecideTransactionRequest req);
    Task<BalanceResponse> GetBalanceAsync(long playerId);
}