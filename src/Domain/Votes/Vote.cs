using FotoTime.Domain.Common;

namespace FotoTime.Domain.Topics;

public sealed class Vote : Entity
{
    private Vote(Guid id, Guid weeklyTopicId, Guid membershipId, Guid photoId, DateTimeOffset castAt) : base(id)
    {
        Guard.AgainstEmpty(weeklyTopicId, nameof(weeklyTopicId));
        Guard.AgainstEmpty(membershipId, nameof(membershipId));
        Guard.AgainstEmpty(photoId, nameof(photoId));

        WeeklyTopicId = weeklyTopicId;
        MembershipId = membershipId;
        PhotoId = photoId;
        CastAt = castAt;
    }

    public Guid WeeklyTopicId { get; }

    public Guid MembershipId { get; private set; }

    public Guid PhotoId { get; private set; }

    public DateTimeOffset CastAt { get; private set; }

    public static Vote Create(Guid id, Guid weeklyTopicId, Guid membershipId, Guid photoId, DateTimeOffset castAt)
        => new(id, weeklyTopicId, membershipId, photoId, castAt);

    public void ReplaceCandidate(Guid photoId, DateTimeOffset castAt)
    {
        Guard.AgainstEmpty(photoId, nameof(photoId));
        PhotoId = photoId;
        CastAt = castAt;
    }
}
