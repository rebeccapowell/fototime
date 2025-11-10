using FotoTime.Application.Invitations;
using Infrastructure.Temporal.Activities;
using Microsoft.Extensions.Logging;
using Temporalio.Common;
using Temporalio.Workflows;

namespace Infrastructure.Temporal.Workflows;

[Workflow]
public interface IInvitationWorkflow
{
    [WorkflowRun]
    public Task RunAsync(InvitationWorkflowRequest request);
}

[Workflow]
public sealed class InvitationWorkflow : IInvitationWorkflow
{
    private static readonly ActivityOptions ActivityOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromMinutes(5),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval = TimeSpan.FromSeconds(10),
            BackoffCoefficient = 2,
            MaximumAttempts = 5
        }
    };

    private static readonly TimeSpan ReminderLeadTime = TimeSpan.FromDays(1);

    [WorkflowRun]
    public async Task RunAsync(InvitationWorkflowRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        Workflow.Logger.LogInformation(
            "Invitation workflow started for invite {InviteId} (expires {ExpiresAt:u})",
            request.InviteId,
            request.ExpiresAt);

        var reminderAt = request.ExpiresAt - ReminderLeadTime;

        if (reminderAt > request.IssuedAt && reminderAt > Workflow.UtcNow)
        {
            await Workflow.DelayAsync(reminderAt - Workflow.UtcNow);

            var context = new InviteEmailContext(
                request.GroupId,
                request.InviteId,
                request.Token,
                request.Email,
                request.ExpiresAt,
                request.IssuedAt);

            await Workflow.ExecuteActivityAsync(
                (IInvitationActivities act) => act.SendReminderAsync(context, default),
                ActivityOptions);

            Workflow.Logger.LogInformation(
                "Reminder email sent for invite {InviteId}",
                request.InviteId);
        }

        if (request.ExpiresAt > Workflow.UtcNow)
        {
            await Workflow.DelayAsync(request.ExpiresAt - Workflow.UtcNow);
        }

        var expiredAt = Workflow.UtcNow;

        await Workflow.ExecuteActivityAsync(
            (IInvitationActivities act) => act.ExpireInviteAsync(request.GroupId, request.InviteId, expiredAt, default),
            ActivityOptions);

        Workflow.Logger.LogInformation(
            "Invite {InviteId} expired at {ExpiredAt:u}",
            request.InviteId,
            expiredAt);
    }
}
