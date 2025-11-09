using FotoTime.Application.Groups;
using FotoTime.Application.Invitations;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Temporal.Activities;

public interface IInvitationActivities
{
    public Task SendReminderAsync(InviteEmailContext context, CancellationToken cancellationToken = default);

    public Task ExpireInviteAsync(Guid groupId, Guid inviteId, DateTimeOffset expiredAt, CancellationToken cancellationToken = default);
}

public sealed class InvitationActivities : IInvitationActivities
{
    private readonly IInviteEmailService _emailService;
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<InvitationActivities> _logger;

    public InvitationActivities(
        IInviteEmailService emailService,
        IGroupRepository groupRepository,
        ILogger<InvitationActivities> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _groupRepository = groupRepository ?? throw new ArgumentNullException(nameof(groupRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task SendReminderAsync(InviteEmailContext context, CancellationToken cancellationToken = default)
        => _emailService.SendReminderAsync(context, cancellationToken);

    public async Task ExpireInviteAsync(Guid groupId, Guid inviteId, DateTimeOffset expiredAt, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.GetByIdAsync(groupId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Group not found for invite expiration.");

        group.ExpireInvite(inviteId, expiredAt);
        await _groupRepository.SaveAsync(group, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Invite {InviteId} for group {GroupId} expired at {ExpiredAt}",
            inviteId,
            groupId,
            expiredAt);
    }
}
