using FotoTime.Application.Invitations;
using Infrastructure.Temporal.Workflows;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

namespace Infrastructure.Temporal;

public sealed class InvitationWorkflowClient : IInvitationWorkflowClient
{
    private readonly ITemporalClient _client;
    private readonly ILogger<InvitationWorkflowClient> _logger;

    public InvitationWorkflowClient(ITemporalClient client, ILogger<InvitationWorkflowClient> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ScheduleAsync(InvitationWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation(
            "Scheduling invitation workflow for invite {InviteId}",
            request.InviteId);

        cancellationToken.ThrowIfCancellationRequested();

        await _client.StartWorkflowAsync(
            (IInvitationWorkflow workflow) => workflow.RunAsync(request),
            new() { TaskQueue = "default", Id = $"invite-{request.InviteId}" })
            .ConfigureAwait(false);
    }
}
