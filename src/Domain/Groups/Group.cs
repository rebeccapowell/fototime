using System.Linq;
using FotoTime.Domain.Challenges;
using FotoTime.Domain.Common;
using FotoTime.Domain.Events;
using FotoTime.Domain.Photos;
using FotoTime.Domain.Topics;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Groups;

public sealed class Group : Entity
{
    private readonly Dictionary<Guid, Membership> _memberships = new();
    private readonly Dictionary<Guid, Challenge> _challenges = new();
    private readonly Dictionary<Guid, WeeklyTopic> _topics = new();
    private readonly Dictionary<string, Invite> _invitesByToken = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EventItem> _eventItems = new();
    private readonly HashSet<Guid> _activeMembershipUsers = new();

    private Group(Guid id, Slug slug, PhotoLimits photoLimits) : base(id)
    {
        Guard.AgainstNull(slug, nameof(slug));
        Guard.AgainstNull(photoLimits, nameof(photoLimits));

        Slug = slug;
        PhotoLimits = photoLimits;
    }

    public Slug Slug { get; private set; }

    public PhotoLimits PhotoLimits { get; private set; }

    public WeeklyTopic? ActiveTopic { get; private set; }

    public IReadOnlyCollection<Membership> Memberships => _memberships.Values;

    public IReadOnlyCollection<WeeklyTopic> Topics => _topics.Values;

    public IReadOnlyCollection<Challenge> Challenges => _challenges.Values;

    public IReadOnlyCollection<Invite> Invites => _invitesByToken.Values;

    public IReadOnlyCollection<EventItem> EventItems => _eventItems.AsReadOnly();

    public static Group Create(Guid id, Slug slug, PhotoLimits? photoLimits = null)
        => new(id, slug, photoLimits ?? PhotoLimits.Default);

    public Membership AddMembership(Guid membershipId, Guid userId, MembershipRole role)
    {
        Guard.AgainstEmpty(membershipId, nameof(membershipId));
        Guard.AgainstEmpty(userId, nameof(userId));

        if (_activeMembershipUsers.Contains(userId))
        {
            throw new InvalidOperationException("User already has an active membership for this group.");
        }

        var membership = new Membership(membershipId, Id, userId, role);
        _memberships.Add(membership.Id, membership);
        if (membership.State == MembershipState.Active)
        {
            _activeMembershipUsers.Add(userId);
        }

        return membership;
    }

    public void SuspendMembership(Guid membershipId)
    {
        if (!_memberships.TryGetValue(membershipId, out var membership))
        {
            throw new InvalidOperationException("Membership does not exist.");
        }

        membership.Suspend();
        _activeMembershipUsers.Remove(membership.UserId);
    }

    public void ReinstateMembership(Guid membershipId)
    {
        if (!_memberships.TryGetValue(membershipId, out var membership))
        {
            throw new InvalidOperationException("Membership does not exist.");
        }

        if (_activeMembershipUsers.Contains(membership.UserId))
        {
            throw new InvalidOperationException("User already has an active membership.");
        }

        membership.Reinstate();
        _activeMembershipUsers.Add(membership.UserId);
    }

    public Invite IssueInvite(Guid inviteId, string token, string email, Guid inviterMembershipId, Period validFor, DateTimeOffset issuedAt)
    {
        if (!_memberships.TryGetValue(inviterMembershipId, out var inviter))
        {
            throw new InvalidOperationException("Inviter must be an existing membership.");
        }

        if (inviter.Role != MembershipRole.Owner)
        {
            throw new InvalidOperationException("Only owners may issue invites.");
        }

        if (_invitesByToken.ContainsKey(token))
        {
            throw new InvalidOperationException("An invite with the provided token already exists.");
        }

        var invite = Invite.Create(inviteId, Id, inviter.Id, token, email, validFor, issuedAt);
        _invitesByToken[token] = invite;

        RaiseDomainEvent(new InviteSent(Id, invite.Id, inviter.Id, invite.Email, invite.ValidFor.End, issuedAt));
        return invite;
    }

    public Membership AcceptInvite(string token, Guid membershipId, Guid userId, DateTimeOffset acceptedAt)
    {
        if (!_invitesByToken.TryGetValue(token, out var invite))
        {
            throw new InvalidOperationException("Invite token not found.");
        }

        invite.Accept(acceptedAt);
        return AddMembership(membershipId, userId, MembershipRole.Member);
    }

    public void ExpireInvite(Guid inviteId, DateTimeOffset expiredAt)
    {
        var invite = _invitesByToken.Values.FirstOrDefault(i => i.Id == inviteId)
            ?? throw new InvalidOperationException("Invite does not exist.");

        invite.Expire(expiredAt);
    }

    public Challenge ProposeChallenge(Guid challengeId, string title, Slug slug)
    {
        if (_challenges.Values.Any(c => c.Slug.Equals(slug)))
        {
            throw new InvalidOperationException("Challenge slugs must be unique within the group.");
        }

        var challenge = Challenge.Propose(challengeId, Id, title, slug);
        _challenges.Add(challenge.Id, challenge);
        return challenge;
    }

    public WeeklyTopic ScheduleWeeklyTopic(Guid topicId, Guid challengeId, Period activePeriod, Period votingPeriod)
    {
        if (!_challenges.TryGetValue(challengeId, out var challenge))
        {
            throw new InvalidOperationException("Challenge must exist to create a topic.");
        }

        activePeriod.EnsureDoesNotOverlap(_topics.Values.Select(t => t.ActivePeriod));

        var topic = WeeklyTopic.Schedule(topicId, Id, challenge, activePeriod, votingPeriod);
        _topics.Add(topic.Id, topic);
        return topic;
    }

    public void StartTopic(Guid topicId, DateTimeOffset startedAt)
    {
        if (!_topics.TryGetValue(topicId, out var topic))
        {
            throw new InvalidOperationException("Topic does not exist.");
        }

        if (ActiveTopic is not null && ActiveTopic.State == WeeklyTopicState.Active)
        {
            throw new InvalidOperationException("Only one topic may be active at a time.");
        }

        var @event = topic.Start(startedAt);
        RaiseDomainEvent(@event);
        ActiveTopic = topic;
    }

    public Photo SubmitPhoto(Guid topicId, Guid photoId, Guid membershipId, ContentSafetyTag tag, DateTimeOffset submittedAt, long fileSizeBytes, int longEdgePixels)
    {
        if (!_topics.TryGetValue(topicId, out var topic))
        {
            throw new InvalidOperationException("Topic does not exist.");
        }

        if (!_memberships.TryGetValue(membershipId, out var membership))
        {
            throw new InvalidOperationException("Membership does not exist.");
        }

        if (membership.State != MembershipState.Active)
        {
            throw new InvalidOperationException("Suspended memberships cannot submit photos.");
        }

        var (photo, domainEvent) = topic.SubmitPhoto(photoId, membership.Id, tag, PhotoLimits, submittedAt, fileSizeBytes, longEdgePixels);
        RaiseDomainEvent(domainEvent);
        return photo;
    }

    public void CloseTopic(Guid topicId, DateTimeOffset closedAt, IReadOnlyDictionary<Guid, int> voteTotals)
    {
        if (!_topics.TryGetValue(topicId, out var topic))
        {
            throw new InvalidOperationException("Topic does not exist.");
        }

        var @event = topic.CloseVoting(closedAt, voteTotals);
        RaiseDomainEvent(@event);
        ActiveTopic = null;
    }

    public Vote CastVote(Guid topicId, Guid voteId, Guid membershipId, Guid photoId, DateTimeOffset castAt)
    {
        if (!_topics.TryGetValue(topicId, out var topic))
        {
            throw new InvalidOperationException("Topic does not exist.");
        }

        if (!_memberships.TryGetValue(membershipId, out var membership))
        {
            throw new InvalidOperationException("Membership does not exist.");
        }

        if (membership.State != MembershipState.Active)
        {
            throw new InvalidOperationException("Suspended memberships cannot vote.");
        }

        return topic.CastVote(voteId, membership.Id, photoId, castAt);
    }

    public EventItem RecordEvent(Guid eventId, string description, DateTimeOffset timestamp)
    {
        var lastTimestamp = _eventItems.LastOrDefault()?.Timestamp;
        var item = EventItem.Create(eventId, Id, description, timestamp, lastTimestamp);
        _eventItems.Add(item);
        return item;
    }

    public void UpdatePhotoLimits(PhotoLimits limits)
    {
        Guard.AgainstNull(limits, nameof(limits));
        PhotoLimits = limits;
    }
}
