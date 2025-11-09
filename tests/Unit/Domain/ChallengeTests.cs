using FotoTime.Domain.Challenges;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class ChallengeTests
{
    [Fact]
    public void Approve_FromProposed_Succeeds()
    {
        var challenge = Challenge.Propose(Guid.NewGuid(), Guid.NewGuid(), "Challenge", Slug.Create("challenge"));
        challenge.Approve();

        Assert.Equal(ChallengeStatus.Approved, challenge.Status);
    }

    [Fact]
    public void Archive_SkippingSteps_Throws()
    {
        var challenge = Challenge.Propose(Guid.NewGuid(), Guid.NewGuid(), "Challenge", Slug.Create("challenge"));

        Assert.Throws<InvalidOperationException>(() => challenge.Archive());
    }
}
