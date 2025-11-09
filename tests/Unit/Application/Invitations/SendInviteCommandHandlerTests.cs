using FotoTime.Application.Common;
using FotoTime.Application.Groups;
using FotoTime.Application.Invitations;
using FotoTime.Domain.Groups;
using FotoTime.Domain.ValueObjects;

namespace Unit.Application.Invitations;

public class SendInviteCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_IssuesInvite_SendsEmailAndSchedulesWorkflow()
    {
        var groupId = Guid.NewGuid();
        var inviterMembershipId = Guid.NewGuid();
        var inviteId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var now = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var group = Group.Create(groupId, Slug.Create("family-group"));
        _ = group.AddMembership(inviterMembershipId, Guid.NewGuid(), MembershipRole.Owner);

        var repository = new FakeGroupRepository(group);
        var tokenGenerator = new StubInviteTokenGenerator("token-123");
        var emailService = new FakeInviteEmailService();
        var workflowClient = new FakeInvitationWorkflowClient();
        var clock = new StubClock(now);
        var idGenerator = new StubIdGenerator(inviteId);

        var handler = new SendInviteCommandHandler(
            repository,
            tokenGenerator,
            emailService,
            workflowClient,
            clock,
            idGenerator);

        var command = new SendInviteCommand(groupId, inviterMembershipId, "child@example.com", TimeSpan.FromDays(3));
        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        var result = await handler.HandleAsync(command, cancellationToken);

        Assert.Equal(inviteId, result.InviteId);
        Assert.Equal("token-123", result.Token);
        Assert.Equal(now.AddDays(3), result.ExpiresAt);
        Assert.True(repository.SaveCalled);

        var invite = Assert.Single(group.Invites);
        Assert.Equal(result.InviteId, invite.Id);
        Assert.Equal("child@example.com", invite.Email);

        var emailContext = Assert.Single(emailService.SentInvites);
        Assert.Equal(result.InviteId, emailContext.InviteId);
        Assert.Equal(result.Token, emailContext.Token);
        Assert.Equal(result.ExpiresAt, emailContext.ExpiresAt);

        var workflowRequest = Assert.Single(workflowClient.Requests);
        Assert.Equal(result.InviteId, workflowRequest.InviteId);
        Assert.Equal(result.Token, workflowRequest.Token);
        Assert.Equal(result.ExpiresAt, workflowRequest.ExpiresAt);
    }

    [Fact]
    public async Task HandleAsync_WhenGroupMissing_Throws()
    {
        var repository = new FakeGroupRepository(null);
        var handler = new SendInviteCommandHandler(
            repository,
            new StubInviteTokenGenerator("token"),
            new FakeInviteEmailService(),
            new FakeInvitationWorkflowClient(),
            new StubClock(DateTimeOffset.UtcNow),
            new StubIdGenerator(Guid.NewGuid()));

        var command = new SendInviteCommand(Guid.NewGuid(), Guid.NewGuid(), "user@example.com", TimeSpan.FromDays(1));

        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        async Task Act() => await handler.HandleAsync(command, cancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(Act);
    }

    [Fact]
    public async Task HandleAsync_WhenValidityNotPositive_Throws()
    {
        var groupId = Guid.NewGuid();
        var inviterMembershipId = Guid.NewGuid();
        var group = Group.Create(groupId, Slug.Create("family-group"));
        _ = group.AddMembership(inviterMembershipId, Guid.NewGuid(), MembershipRole.Owner);

        var handler = new SendInviteCommandHandler(
            new FakeGroupRepository(group),
            new StubInviteTokenGenerator("token"),
            new FakeInviteEmailService(),
            new FakeInvitationWorkflowClient(),
            new StubClock(DateTimeOffset.UtcNow),
            new StubIdGenerator(Guid.NewGuid()));

        var command = new SendInviteCommand(groupId, inviterMembershipId, "child@example.com", TimeSpan.Zero);

        var cancellationToken = TestContext.Current?.CancellationToken ?? default;

        async Task Act() => await handler.HandleAsync(command, cancellationToken);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(Act);
    }

    [Fact]
    public async Task HandleAsync_TrimsEmailBeforeIssuingInvite()
    {
        var groupId = Guid.NewGuid();
        var inviterMembershipId = Guid.NewGuid();
        var group = Group.Create(groupId, Slug.Create("family-group"));
        _ = group.AddMembership(inviterMembershipId, Guid.NewGuid(), MembershipRole.Owner);

        var emailService = new FakeInviteEmailService();
        var handler = new SendInviteCommandHandler(
            new FakeGroupRepository(group),
            new StubInviteTokenGenerator("token"),
            emailService,
            new FakeInvitationWorkflowClient(),
            new StubClock(DateTimeOffset.UtcNow),
            new StubIdGenerator(Guid.NewGuid()));

        var command = new SendInviteCommand(groupId, inviterMembershipId, "  child@example.com  ", TimeSpan.FromDays(1));

        var result = await handler.HandleAsync(command, CancellationToken.None);

        var invite = Assert.Single(group.Invites);
        Assert.Equal("child@example.com", invite.Email);
        Assert.Equal(invite.Id, result.InviteId);
        var sentInvite = Assert.Single(emailService.SentInvites);
        Assert.Equal("child@example.com", sentInvite.Email);
    }

    [Fact]
    public async Task HandleAsync_WhenEmailWhitespace_Throws()
    {
        var groupId = Guid.NewGuid();
        var inviterMembershipId = Guid.NewGuid();
        var group = Group.Create(groupId, Slug.Create("family-group"));
        _ = group.AddMembership(inviterMembershipId, Guid.NewGuid(), MembershipRole.Owner);

        var handler = new SendInviteCommandHandler(
            new FakeGroupRepository(group),
            new StubInviteTokenGenerator("token"),
            new FakeInviteEmailService(),
            new FakeInvitationWorkflowClient(),
            new StubClock(DateTimeOffset.UtcNow),
            new StubIdGenerator(Guid.NewGuid()));

        var command = new SendInviteCommand(groupId, inviterMembershipId, "   ", TimeSpan.FromDays(1));

        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(command, CancellationToken.None));
    }

    private sealed class FakeGroupRepository : IGroupRepository
    {
        private readonly Group? _group;

        public FakeGroupRepository(Group? group)
        {
            _group = group;
        }

        public bool SaveCalled { get; private set; }

        public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
            => Task.FromResult(_group);

        public Task SaveAsync(Group group, CancellationToken cancellationToken = default)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class StubInviteTokenGenerator : IInviteTokenGenerator
    {
        private readonly string _token;

        public StubInviteTokenGenerator(string token) => _token = token;

        public string CreateToken() => _token;
    }

    private sealed class FakeInviteEmailService : IInviteEmailService
    {
        public List<InviteEmailContext> SentInvites { get; } = new();

        public List<InviteEmailContext> SentReminders { get; } = new();

        public Task SendInviteAsync(InviteEmailContext context, CancellationToken cancellationToken = default)
        {
            SentInvites.Add(context);
            return Task.CompletedTask;
        }

        public Task SendReminderAsync(InviteEmailContext context, CancellationToken cancellationToken = default)
        {
            SentReminders.Add(context);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeInvitationWorkflowClient : IInvitationWorkflowClient
    {
        public List<InvitationWorkflowRequest> Requests { get; } = new();

        public Task ScheduleAsync(InvitationWorkflowRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class StubClock : IClock
    {
        public StubClock(DateTimeOffset now) => UtcNow = now;

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class StubIdGenerator : IIdGenerator
    {
        private readonly Guid _id;

        public StubIdGenerator(Guid id) => _id = id;

        public Guid NewGuid() => _id;
    }
}
