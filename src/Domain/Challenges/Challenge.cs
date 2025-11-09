using FotoTime.Domain.Common;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Challenges;

public sealed class Challenge : Entity
{
    private Challenge(Guid id, Guid groupId, string title, Slug slug) : base(id)
    {
        Guard.AgainstEmpty(groupId, nameof(groupId));
        Guard.AgainstNullOrWhiteSpace(title, nameof(title));
        Guard.AgainstNull(slug, nameof(slug));

        GroupId = groupId;
        Title = title.Trim();
        Slug = slug;
        Status = ChallengeStatus.Proposed;
    }

    public Guid GroupId { get; }

    public string Title { get; private set; }

    public Slug Slug { get; }

    public ChallengeStatus Status { get; private set; }

    public static Challenge Propose(Guid id, Guid groupId, string title, Slug slug) => new(id, groupId, title, slug);

    public void Approve()
    {
        if (Status != ChallengeStatus.Proposed)
        {
            throw new InvalidOperationException("Only proposed challenges can be approved.");
        }

        Status = ChallengeStatus.Approved;
    }

    public void Schedule()
    {
        if (Status != ChallengeStatus.Approved)
        {
            throw new InvalidOperationException("Only approved challenges can be scheduled.");
        }

        Status = ChallengeStatus.Scheduled;
    }

    public void MarkUsed()
    {
        if (Status != ChallengeStatus.Scheduled)
        {
            throw new InvalidOperationException("Only scheduled challenges can be marked as used.");
        }

        Status = ChallengeStatus.Used;
    }

    public void Archive()
    {
        if (Status != ChallengeStatus.Used)
        {
            throw new InvalidOperationException("Only used challenges can be archived.");
        }

        Status = ChallengeStatus.Archived;
    }
}

public enum ChallengeStatus
{
    Proposed,
    Approved,
    Scheduled,
    Used,
    Archived
}
