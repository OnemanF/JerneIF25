using System.Collections.Generic;
using System.Threading.Tasks;
using api.DTOs.Subscriptions;

namespace api.Services;

public interface ISubscriptionsService
{
    Task<IReadOnlyList<SubscriptionDto>> ListAsync(long playerId);
    Task<CreateSubscriptionResponse> CreateAsync(CreateSubscriptionRequest req);
    Task<SubscriptionDto> CancelAsync(CancelSubscriptionRequest req);
}