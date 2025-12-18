using System;

namespace api.DTOs.Subscriptions;

public sealed record CreateSubscriptionResponse(
    long Id,
    long PlayerId,
    short[] Numbers,
    int RemainingWeeks,
    bool IsActive,
    DateTime StartedAt
);