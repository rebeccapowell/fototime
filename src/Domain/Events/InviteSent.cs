using FotoTime.Domain.Common;

namespace FotoTime.Domain.Events;

public sealed record InviteSent(
    Guid GroupId,
    Guid InviteId,
    Guid InviterMembershipId,
    string Email,
    DateTimeOffset ExpiresAt,
    DateTimeOffset OccurredOn) : IDomainEvent;
