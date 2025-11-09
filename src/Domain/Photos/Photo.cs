using FotoTime.Domain.Common;
using FotoTime.Domain.Comments;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Photos;

public sealed class Photo : Entity
{
    private readonly HashSet<Guid> _likes = new();
    private readonly List<Comment> _comments = new();

    private Photo(
        Guid id,
        Guid weeklyTopicId,
        Guid membershipId,
        ContentSafetyTag tag,
        DateTimeOffset submittedAt,
        long fileSizeBytes,
        int longEdgePixels) : base(id)
    {
        Guard.AgainstEmpty(weeklyTopicId, nameof(weeklyTopicId));
        Guard.AgainstEmpty(membershipId, nameof(membershipId));

        WeeklyTopicId = weeklyTopicId;
        MembershipId = membershipId;
        Tag = tag;
        SubmittedAt = submittedAt;
        FileSizeBytes = fileSizeBytes;
        LongEdgePixels = longEdgePixels;
    }

    public Guid WeeklyTopicId { get; }

    public Guid MembershipId { get; }

    public ContentSafetyTag Tag { get; private set; }

    public DateTimeOffset SubmittedAt { get; }

    public long FileSizeBytes { get; }

    public int LongEdgePixels { get; }

    public IReadOnlyCollection<Guid> Likes => _likes;

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    public static Photo Create(
        Guid id,
        Guid weeklyTopicId,
        Guid membershipId,
        ContentSafetyTag tag,
        DateTimeOffset submittedAt,
        long fileSizeBytes,
        int longEdgePixels,
        PhotoLimits limits)
    {
        Guard.AgainstNull(limits, nameof(limits));
        Guard.AgainstNegativeOrZero(fileSizeBytes, nameof(fileSizeBytes));
        Guard.AgainstNegativeOrZero(longEdgePixels, nameof(longEdgePixels));

        if (fileSizeBytes > limits.MaxFileSizeBytes)
        {
            throw new InvalidOperationException("Photo exceeds the configured size limit.");
        }

        if (longEdgePixels > limits.MaxLongEdgePixels)
        {
            throw new InvalidOperationException("Photo exceeds the configured resolution limit.");
        }

        return new Photo(id, weeklyTopicId, membershipId, tag, submittedAt, fileSizeBytes, longEdgePixels);
    }

    public bool TryLike(Guid membershipId)
    {
        Guard.AgainstEmpty(membershipId, nameof(membershipId));
        return _likes.Add(membershipId);
    }

    public void AddComment(Comment comment)
    {
        Guard.AgainstNull(comment, nameof(comment));
        if (comment.WeeklyTopicId != WeeklyTopicId)
        {
            throw new InvalidOperationException("Comments must belong to the same topic as the photo.");
        }

        _comments.Add(comment);
    }

    public void ChangeSafetyTag(ContentSafetyTag next, bool hasModeratorApproval)
    {
        Tag.EnsureTransition(next, hasModeratorApproval);
        Tag = next;
    }
}
