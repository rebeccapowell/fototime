namespace FotoTime.Application.Invitations;

public sealed record InviteEmailContext(
    Guid GroupId,
    Guid InviteId,
    string Token,
    string Email,
    DateTimeOffset ExpiresAt,
    DateTimeOffset IssuedAt);

public interface IInviteEmailService
{
    public Task SendInviteAsync(InviteEmailContext context, CancellationToken cancellationToken = default);

    public Task SendReminderAsync(InviteEmailContext context, CancellationToken cancellationToken = default);
}
