using FotoTime.Domain.Common;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Events;

public sealed record TopicStarted(
    Guid GroupId,
    Guid WeeklyTopicId,
    Guid ChallengeId,
    Period ActivePeriod,
    DateTimeOffset OccurredOn) : IDomainEvent;
