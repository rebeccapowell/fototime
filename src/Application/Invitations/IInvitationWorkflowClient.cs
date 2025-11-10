namespace FotoTime.Application.Invitations;

public sealed record InvitationWorkflowRequest(
    Guid GroupId,
    Guid InviteId,
    string Token,
    string Email,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt);

public interface IInvitationWorkflowClient
{
    public Task ScheduleAsync(InvitationWorkflowRequest request, CancellationToken cancellationToken = default);
}
