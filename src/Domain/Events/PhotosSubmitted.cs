using FotoTime.Domain.Common;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Events;

public sealed record PhotosSubmitted(
    Guid GroupId,
    Guid WeeklyTopicId,
    Guid PhotoId,
    Guid MembershipId,
    ContentSafetyTag Tag,
    DateTimeOffset OccurredOn) : IDomainEvent;
