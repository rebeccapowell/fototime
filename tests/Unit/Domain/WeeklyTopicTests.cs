using FotoTime.Domain.Challenges;
using FotoTime.Domain.Topics;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class WeeklyTopicTests
{
    [Fact]
    public void SubmitPhoto_WhenLimitExceeded_Throws()
    {
        var groupId = Guid.NewGuid();
        var challenge = Challenge.Propose(Guid.NewGuid(), groupId, "Challenge", Slug.Create("challenge"));
        challenge.Approve();
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var voting = Period.Create(period.End, period.End.AddHours(1));
        var topic = WeeklyTopic.Schedule(Guid.NewGuid(), groupId, challenge, period, voting);
        var limits = PhotoLimits.Create(1, PhotoLimits.MaximumFileSizeBytes, PhotoLimits.MaximumResolutionPixels);
        topic.Start(period.Start.AddMinutes(1));

        var membershipId = Guid.NewGuid();
        topic.SubmitPhoto(Guid.NewGuid(), membershipId, ContentSafetyTag.Safe, limits, period.Start.AddMinutes(2), 100, 100);

        Assert.Throws<InvalidOperationException>(() =>
            topic.SubmitPhoto(Guid.NewGuid(), membershipId, ContentSafetyTag.Safe, limits, period.Start.AddMinutes(3), 100, 100));
    }

    [Fact]
    public void AddSideQuest_WithDuplicateSlug_Throws()
    {
        var groupId = Guid.NewGuid();
        var challenge = Challenge.Propose(Guid.NewGuid(), groupId, "Challenge", Slug.Create("challenge"));
        challenge.Approve();
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var voting = Period.Create(period.End, period.End.AddHours(1));
        var topic = WeeklyTopic.Schedule(Guid.NewGuid(), groupId, challenge, period, voting);

        topic.AddSideQuest(Guid.NewGuid(), Slug.Create("quest"), "Quest");

        Assert.Throws<InvalidOperationException>(() => topic.AddSideQuest(Guid.NewGuid(), Slug.Create("quest"), "Duplicate"));
    }
}
