using FotoTime.Domain.Groups;

namespace FotoTime.Unit.Domain;

public class EventItemTests
{
    [Fact]
    public void Create_WhenTimestampRegresses_Throws()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<InvalidOperationException>(() => EventItem.Create(Guid.NewGuid(), Guid.NewGuid(), "Created", now.AddMinutes(-1), now));
    }
}
