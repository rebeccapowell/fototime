using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class PeriodTests
{
    [Fact]
    public void Create_WhenEndBeforeStart_Throws()
    {
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(-1);

        Assert.Throws<ArgumentException>(() => Period.Create(start, end));
    }

    [Fact]
    public void Contains_WhenTimestampInside_ReturnsTrue()
    {
        var start = DateTimeOffset.UtcNow;
        var period = Period.Create(start, start.AddHours(1));

        Assert.True(period.Contains(start.AddMinutes(30)));
    }

    [Fact]
    public void EnsureDoesNotOverlap_WhenOverlap_Throws()
    {
        var start = DateTimeOffset.UtcNow;
        var first = Period.Create(start, start.AddHours(2));
        var second = Period.Create(start.AddHours(1), start.AddHours(3));

        Assert.Throws<InvalidOperationException>(() => second.EnsureDoesNotOverlap(new[] { first }));
    }
}
