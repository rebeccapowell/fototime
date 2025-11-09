using FotoTime.Domain.Comments;

namespace FotoTime.Unit.Domain;

public class CommentTests
{
    [Fact]
    public void Create_WhenWhitespace_Throws()
    {
        Assert.Throws<ArgumentException>(() => Comment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void SoftDelete_SetsFlags()
    {
        var comment = Comment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Nice", DateTimeOffset.UtcNow);
        var deletedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        comment.SoftDelete(deletedAt);

        Assert.True(comment.IsDeleted);
        Assert.Equal(deletedAt, comment.DeletedAt);
    }
}
