using FotoTime.Domain.Profiles;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class ProfileTests
{
    [Fact]
    public void ChangePreferredContentSafety_RequiresApproval()
    {
        var profile = new Profile(Guid.NewGuid(), Guid.NewGuid(), DisplayName.Create("Alice"), ContentSafetyTag.Restricted);

        Assert.Throws<InvalidOperationException>(() => profile.ChangePreferredContentSafety(ContentSafetyTag.Safe, false));
    }

    [Fact]
    public void UpdateBio_TrimsWhitespace()
    {
        var profile = new Profile(Guid.NewGuid(), Guid.NewGuid(), DisplayName.Create("Alice"), ContentSafetyTag.Safe);
        profile.UpdateBio("  Hello world  ");

        Assert.Equal("Hello world", profile.Bio);
    }
}
