using FotoTime.Domain.Groups;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class InviteTests
{
    [Fact]
    public void Accept_WhenOutsidePeriod_Throws()
    {
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var invite = Invite.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "token", "user@example.com", period, DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => invite.Accept(period.End.AddHours(1)));
    }

    [Fact]
    public void Expire_AfterAcceptance_NoEffect()
    {
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1));
        var invite = Invite.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "token", "user@example.com", period, DateTimeOffset.UtcNow);
        var acceptTime = period.Start.AddMinutes(10);
        invite.Accept(acceptTime);

        Assert.Throws<InvalidOperationException>(() => invite.Accept(acceptTime));
        invite.Expire(period.End.AddHours(1));

        Assert.Equal(InviteStatus.Accepted, invite.Status);
    }
}
