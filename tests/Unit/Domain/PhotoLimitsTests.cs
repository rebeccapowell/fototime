using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class PhotoLimitsTests
{
    [Fact]
    public void Create_WhenWithinBounds_Succeeds()
    {
        var limits = PhotoLimits.Create(2, 5 * 1024 * 1024, 4000);

        Assert.Equal(2, limits.MaxSubmissionsPerMember);
    }

    [Theory]
    [InlineData(0, PhotoLimits.MaximumFileSizeBytes, PhotoLimits.MaximumResolutionPixels)]
    [InlineData(1, PhotoLimits.MaximumFileSizeBytes + 1, PhotoLimits.MaximumResolutionPixels)]
    [InlineData(1, PhotoLimits.MaximumFileSizeBytes, PhotoLimits.MaximumResolutionPixels + 1)]
    public void Create_WhenOutOfBounds_Throws(int submissions, long fileSize, int resolution)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PhotoLimits.Create(submissions, fileSize, resolution));
    }
}
