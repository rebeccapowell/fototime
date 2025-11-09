using FotoTime.Domain.Comments;
using FotoTime.Domain.Photos;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class PhotoTests
{
    [Fact]
    public void Create_WhenExceedingLimits_Throws()
    {
        var limits = PhotoLimits.Create(1, 100, 100);

        Assert.Throws<InvalidOperationException>(() => Photo.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ContentSafetyTag.Safe, DateTimeOffset.UtcNow, 200, 50, limits));
        Assert.Throws<InvalidOperationException>(() => Photo.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ContentSafetyTag.Safe, DateTimeOffset.UtcNow, 50, 200, limits));
    }

    [Fact]
    public void TryLike_PreventsDuplicateLikes()
    {
        var limits = PhotoLimits.Create(1, 100, 100);
        var photo = Photo.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ContentSafetyTag.Safe, DateTimeOffset.UtcNow, 50, 50, limits);
        var memberId = Guid.NewGuid();

        Assert.True(photo.TryLike(memberId));
        Assert.False(photo.TryLike(memberId));
    }

    [Fact]
    public void AddComment_RequiresMatchingTopic()
    {
        var limits = PhotoLimits.Create(1, 100, 100);
        var photo = Photo.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ContentSafetyTag.Safe, DateTimeOffset.UtcNow, 50, 50, limits);
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), photo.WeeklyTopicId, Guid.NewGuid(), "Nice shot!", DateTimeOffset.UtcNow);
        photo.AddComment(comment);

        var mismatchedComment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Oops", DateTimeOffset.UtcNow);
        Assert.Throws<InvalidOperationException>(() => photo.AddComment(mismatchedComment));
    }
}
