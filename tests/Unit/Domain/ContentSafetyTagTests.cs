using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class ContentSafetyTagTests
{
    [Fact]
    public void EnsureTransition_WhenUpgrading_Allows()
    {
        ContentSafetyTag.Safe.EnsureTransition(ContentSafetyTag.Mature, hasModeratorApproval: false);
    }

    [Fact]
    public void EnsureTransition_WhenDowngradingWithoutApproval_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => ContentSafetyTag.Restricted.EnsureTransition(ContentSafetyTag.Safe, false));
    }

    [Fact]
    public void EnsureTransition_WhenDowngradingWithApproval_Allows()
    {
        ContentSafetyTag.Restricted.EnsureTransition(ContentSafetyTag.Mature, true);
    }
}
