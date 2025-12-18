namespace api.DTOs.Subscriptions;

public sealed record CreateSubscriptionRequest(long PlayerId, int[] Numbers, int RemainingWeeks);