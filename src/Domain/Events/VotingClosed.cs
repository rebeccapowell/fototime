using FotoTime.Domain.Common;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Events;

public sealed record VotingClosed(
    Guid GroupId,
    Guid WeeklyTopicId,
    Period VotingPeriod,
    IReadOnlyDictionary<Guid, int> VoteTotals,
    DateTimeOffset OccurredOn) : IDomainEvent;
