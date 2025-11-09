using FotoTime.Application.Common;
using FotoTime.Application.Groups;
using FotoTime.Domain.Groups;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Application.Invitations;

public sealed record SendInviteCommand(
    Guid GroupId,
    Guid InviterMembershipId,
    string Email,
    TimeSpan ValidFor);

public sealed record SendInviteResult(
    Guid InviteId,
    string Token,
    DateTimeOffset ExpiresAt);

public sealed class SendInviteCommandHandler
{
    private readonly IGroupRepository _groupRepository;
    private readonly IInviteTokenGenerator _tokenGenerator;
    private readonly IInviteEmailService _emailService;
    private readonly IInvitationWorkflowClient _workflowClient;
    private readonly IClock _clock;
    private readonly IIdGenerator _idGenerator;

    public SendInviteCommandHandler(
        IGroupRepository groupRepository,
        IInviteTokenGenerator tokenGenerator,
        IInviteEmailService emailService,
        IInvitationWorkflowClient workflowClient,
        IClock clock,
        IIdGenerator idGenerator)
    {
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _tokenGenerator = tokenGenerator ?? throw new ArgumentNullException(nameof(tokenGenerator));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _workflowClient = workflowClient ?? throw new ArgumentNullException(nameof(workflowClient));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
    }

    public async Task<SendInviteResult> HandleAsync(SendInviteCommand command, CancellationToken cancellationToken = default)
    {
        if (command == null)
        {
            throw new ArgumentNullException(nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new ArgumentException("Email is required.", nameof(command));
        }

        if (command.ValidFor <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "Invite validity must be greater than zero.");
        }

        var email = command.Email.Trim();

        if (email.Length == 0)
        {
            throw new ArgumentException("Email is required.", nameof(command));
        }

        var group = await _groupRepository.GetByIdAsync(command.GroupId, cancellationToken)
            ?? throw new InvalidOperationException("Group not found.");

        var issuedAt = _clock.UtcNow;
        var expiresAt = issuedAt.Add(command.ValidFor);
        var token = _tokenGenerator.CreateToken();
        var inviteId = _idGenerator.NewGuid();
        var validPeriod = Period.Create(issuedAt, expiresAt);

        var invite = group.IssueInvite(
            inviteId,
            token,
            email,
            command.InviterMembershipId,
            validPeriod,
            issuedAt);

        await _groupRepository.SaveAsync(group, cancellationToken).ConfigureAwait(false);

        var context = new InviteEmailContext(group.Id, invite.Id, token, invite.Email, invite.ValidFor.End, invite.CreatedAt);

        await _emailService.SendInviteAsync(context, cancellationToken).ConfigureAwait(false);

        var workflowRequest = new InvitationWorkflowRequest(
            group.Id,
            invite.Id,
            token,
            invite.Email,
            invite.CreatedAt,
            invite.ValidFor.End);

        await _workflowClient.ScheduleAsync(workflowRequest, cancellationToken).ConfigureAwait(false);

        return new SendInviteResult(invite.Id, invite.Token, invite.ValidFor.End);
    }
}
