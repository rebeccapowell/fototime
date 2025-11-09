using System.Linq;
using FotoTime.Domain.Challenges;
using FotoTime.Domain.Common;
using FotoTime.Domain.Events;
using FotoTime.Domain.Photos;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Topics;

public sealed class WeeklyTopic : Entity
{
    private readonly Dictionary<Guid, int> _submissionsPerMember = new();
    private readonly Dictionary<Guid, SideQuest> _sideQuests = new();
    private readonly Dictionary<Guid, Vote> _votes = new();

    private WeeklyTopic(Guid id, Guid groupId, Guid challengeId, Period activePeriod, Period votingPeriod) : base(id)
    {
        Guard.AgainstEmpty(groupId, nameof(groupId));
        Guard.AgainstEmpty(challengeId, nameof(challengeId));
        Guard.AgainstNull(activePeriod, nameof(activePeriod));
        Guard.AgainstNull(votingPeriod, nameof(votingPeriod));

        if (votingPeriod.Start < activePeriod.End)
        {
            throw new ArgumentException("Voting must open no earlier than the end of the active period.", nameof(votingPeriod));
        }

        GroupId = groupId;
        ChallengeId = challengeId;
        ActivePeriod = activePeriod;
        VotingPeriod = votingPeriod;
        State = WeeklyTopicState.Scheduled;
    }

    public Guid GroupId { get; }

    public Guid ChallengeId { get; }

    public Period ActivePeriod { get; private set; }

    public Period VotingPeriod { get; private set; }

    public WeeklyTopicState State { get; private set; }

    public IReadOnlyDictionary<Guid, int> SubmissionsPerMember => _submissionsPerMember;

    public IReadOnlyDictionary<Guid, SideQuest> SideQuests => _sideQuests;

    public IReadOnlyDictionary<Guid, Vote> Votes => _votes;

    public static WeeklyTopic Schedule(Guid id, Guid groupId, Challenge challenge, Period activePeriod, Period votingPeriod)
    {
        Guard.AgainstNull(challenge, nameof(challenge));
        if (challenge.Status == ChallengeStatus.Approved)
        {
            challenge.Schedule();
        }
        else if (challenge.Status != ChallengeStatus.Scheduled)
        {
            throw new InvalidOperationException("Weekly topics must be created from an approved challenge.");
        }
        return new WeeklyTopic(id, groupId, challenge.Id, activePeriod, votingPeriod);
    }

    public TopicStarted Start(DateTimeOffset startedAt)
    {
        if (State != WeeklyTopicState.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled topics can be started.");
        }

        if (!ActivePeriod.Contains(startedAt))
        {
            throw new InvalidOperationException("Topics may only start within their active period.");
        }

        State = WeeklyTopicState.Active;
        return new TopicStarted(GroupId, Id, ChallengeId, ActivePeriod, startedAt);
    }

    public (Photo Photo, PhotosSubmitted DomainEvent) SubmitPhoto(
        Guid photoId,
        Guid membershipId,
        ContentSafetyTag tag,
        PhotoLimits limits,
        DateTimeOffset submittedAt,
        long fileSizeBytes,
        int longEdgePixels)
    {
        if (State != WeeklyTopicState.Active)
        {
            throw new InvalidOperationException("Photos may only be submitted while the topic is active.");
        }

        if (!ActivePeriod.Contains(submittedAt))
        {
            throw new InvalidOperationException("Submissions must occur within the active period.");
        }

        if (!_submissionsPerMember.TryGetValue(membershipId, out var count))
        {
            count = 0;
        }

        if (count >= limits.MaxSubmissionsPerMember)
        {
            throw new InvalidOperationException("Member has reached the submission limit for this topic.");
        }

        var photo = Photo.Create(photoId, Id, membershipId, tag, submittedAt, fileSizeBytes, longEdgePixels, limits);
        _submissionsPerMember[membershipId] = count + 1;

        var @event = new PhotosSubmitted(GroupId, Id, photo.Id, membershipId, tag, submittedAt);
        return (photo, @event);
    }

    public VotingClosed CloseVoting(DateTimeOffset closedAt, IReadOnlyDictionary<Guid, int> voteTotals)
    {
        if (State != WeeklyTopicState.Active)
        {
            throw new InvalidOperationException("Only active topics can have voting closed.");
        }

        if (!VotingPeriod.Contains(closedAt))
        {
            throw new InvalidOperationException("Voting can only close within the voting window.");
        }

        State = WeeklyTopicState.Closed;
        return new VotingClosed(GroupId, Id, VotingPeriod, voteTotals, closedAt);
    }

    public SideQuest AddSideQuest(Guid id, Slug slug, string title)
    {
        if (_sideQuests.Values.Any(q => q.Slug.Equals(slug)))
        {
            throw new InvalidOperationException("Side quest slugs must be unique within the topic.");
        }

        var quest = new SideQuest(id, Id, slug, title);
        _sideQuests.Add(quest.Id, quest);
        return quest;
    }

    public Vote CastVote(Guid voteId, Guid membershipId, Guid photoId, DateTimeOffset castAt)
    {
        if (State != WeeklyTopicState.Active)
        {
            throw new InvalidOperationException("Voting is only permitted while the topic is active.");
        }

        if (!VotingPeriod.Contains(castAt))
        {
            throw new InvalidOperationException("Votes must be cast within the voting window.");
        }

        if (_votes.TryGetValue(membershipId, out var existingVote))
        {
            existingVote.ReplaceCandidate(photoId, castAt);
            return existingVote;
        }

        var vote = Vote.Create(voteId, Id, membershipId, photoId, castAt);
        _votes[membershipId] = vote;
        return vote;
    }
}

public enum WeeklyTopicState
{
    Scheduled,
    Active,
    Closed
}
