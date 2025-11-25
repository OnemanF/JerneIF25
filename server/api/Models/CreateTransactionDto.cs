using System.ComponentModel.DataAnnotations;

namespace api.Models;

public record CreateTransactionDto(long PlayerId, decimal AmountDkk, string? MobilePayRef);