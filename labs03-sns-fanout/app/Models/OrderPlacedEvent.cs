namespace FlociSnsDemo.Models;

public sealed record OrderPlacedEvent(
    string EventType,
    int OrderId,
    string CustomerId,
    decimal Total,
    DateTime OccurredAtUtc
);
