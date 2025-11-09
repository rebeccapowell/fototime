using FotoTime.Domain.Common;

namespace FotoTime.Domain.Groups;

public sealed class EventItem : Entity
{
    private EventItem(Guid id, Guid aggregateId, string description, DateTimeOffset timestamp) : base(id)
    {
        Guard.AgainstEmpty(aggregateId, nameof(aggregateId));
        Guard.AgainstNullOrWhiteSpace(description, nameof(description));

        AggregateId = aggregateId;
        Description = description.Trim();
        Timestamp = timestamp;
    }

    public Guid AggregateId { get; }

    public string Description { get; }

    public DateTimeOffset Timestamp { get; }

    public static EventItem Create(Guid id, Guid aggregateId, string description, DateTimeOffset timestamp, DateTimeOffset? lastTimestamp)
    {
        if (lastTimestamp is not null && timestamp < lastTimestamp)
        {
            throw new InvalidOperationException("Event timestamps must be monotonic.");
        }

        return new EventItem(id, aggregateId, description, timestamp);
    }
}
