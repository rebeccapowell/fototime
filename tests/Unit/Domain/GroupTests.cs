using System.Linq;
using FotoTime.Domain.Challenges;
using FotoTime.Domain.Events;
using FotoTime.Domain.Groups;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Unit.Domain;

public class GroupTests
{
    private static readonly Slug TestSlug = Slug.Create("foto-time");

    [Fact]
    public void AddMembership_WhenUserAlreadyMember_Throws()
    {
        var group = Group.Create(Guid.NewGuid(), TestSlug);
        var userId = Guid.NewGuid();
        group.AddMembership(Guid.NewGuid(), userId, MembershipRole.Member);

        Assert.Throws<InvalidOperationException>(() => group.AddMembership(Guid.NewGuid(), userId, MembershipRole.Member));
    }

    [Fact]
    public void IssueInvite_RaisesDomainEvent()
    {
        var group = Group.Create(Guid.NewGuid(), TestSlug);
        var owner = group.AddMembership(Guid.NewGuid(), Guid.NewGuid(), MembershipRole.Owner);
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(2));

        group.IssueInvite(Guid.NewGuid(), "token", "person@example.com", owner.Id, period, DateTimeOffset.UtcNow);

        var @event = Assert.IsType<InviteSent>(group.DomainEvents.Single());
        Assert.Equal(owner.Id, @event.InviterMembershipId);
    }

    [Fact]
    public void StartTopic_WhenAnotherActive_Throws()
    {
        var group = Group.Create(Guid.NewGuid(), TestSlug);
        var challenge = group.ProposeChallenge(Guid.NewGuid(), "Challenge", Slug.Create("challenge-1"));
        challenge.Approve();
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
        var voting = Period.Create(period.End, period.End.AddDays(1));
        var firstTopic = group.ScheduleWeeklyTopic(Guid.NewGuid(), challenge.Id, period, voting);
        group.StartTopic(firstTopic.Id, period.Start.AddMinutes(1));

        var secondChallenge = group.ProposeChallenge(Guid.NewGuid(), "Second", Slug.Create("challenge-2"));
        secondChallenge.Approve();
        var secondPeriod = Period.Create(period.End.AddDays(1), period.End.AddDays(8));
        var secondVoting = Period.Create(secondPeriod.End, secondPeriod.End.AddDays(1));
        var secondTopic = group.ScheduleWeeklyTopic(Guid.NewGuid(), secondChallenge.Id, secondPeriod, secondVoting);

        Assert.Throws<InvalidOperationException>(() => group.StartTopic(secondTopic.Id, secondPeriod.Start.AddMinutes(1)));
    }

    [Fact]
    public void SubmitPhoto_WhenWithinLimits_AddsEvent()
    {
        var group = Group.Create(Guid.NewGuid(), TestSlug, PhotoLimits.Create(2, 1_000_000, 4000));
        var member = group.AddMembership(Guid.NewGuid(), Guid.NewGuid(), MembershipRole.Member);
        var challenge = group.ProposeChallenge(Guid.NewGuid(), "Challenge", Slug.Create("challenge-1"));
        challenge.Approve();
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
        var voting = Period.Create(period.End, period.End.AddDays(1));
        var topic = group.ScheduleWeeklyTopic(Guid.NewGuid(), challenge.Id, period, voting);
        group.StartTopic(topic.Id, period.Start.AddMinutes(1));

        group.SubmitPhoto(topic.Id, Guid.NewGuid(), member.Id, ContentSafetyTag.Safe, period.Start.AddMinutes(2), 500_000, 2000);

        Assert.Contains(group.DomainEvents, e => e is PhotosSubmitted);
    }

    [Fact]
    public void CloseTopic_RaisesVotingClosedEvent()
    {
        var group = Group.Create(Guid.NewGuid(), TestSlug);
        var challenge = group.ProposeChallenge(Guid.NewGuid(), "Challenge", Slug.Create("challenge-1"));
        challenge.Approve();
        var period = Period.Create(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));
        var voting = Period.Create(period.End, period.End.AddDays(1));
        var topic = group.ScheduleWeeklyTopic(Guid.NewGuid(), challenge.Id, period, voting);
        group.StartTopic(topic.Id, period.Start.AddMinutes(1));

        group.CloseTopic(topic.Id, voting.Start.AddHours(1), new Dictionary<Guid, int>());

        Assert.Contains(group.DomainEvents, e => e is VotingClosed);
    }
}
