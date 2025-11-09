using FotoTime.Domain.Topics;

namespace FotoTime.Unit.Domain;

public class VoteTests
{
    [Fact]
    public void ReplaceCandidate_UpdatesPhoto()
    {
        var vote = Vote.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        var newPhotoId = Guid.NewGuid();
        var updatedAt = DateTimeOffset.UtcNow.AddMinutes(1);

        vote.ReplaceCandidate(newPhotoId, updatedAt);

        Assert.Equal(newPhotoId, vote.PhotoId);
        Assert.Equal(updatedAt, vote.CastAt);
    }
}
