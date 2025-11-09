using System.Linq;
using FotoTime.Application.Invitations;
using Infrastructure.Temporal.Activities;
using Infrastructure.Temporal.Workflows;
using Temporalio.Activities;
using Temporalio.Client;
using Temporalio.Testing;
using Temporalio.Worker;
using Xunit;

namespace Unit.Infrastructure.Temporal;

public class InvitationWorkflowTests
{
    [Fact]
    public async Task RunAsync_WithReminder_SendsReminderAndExpires()
    {
        var environment = await TryStartTimeSkippingAsync();
        if (environment is null)
        {
            return;
        }

        await using var environmentLifetime = environment;
        var activities = new RecordingInvitationActivities();
        var taskQueue = $"invitation-workflow-tests-{Guid.NewGuid():N}";

        using var worker = CreateWorker(environment, taskQueue, activities);

        var cancellationToken = TestContext.Current?.CancellationToken ?? CancellationToken.None;

        await worker.ExecuteAsync(async () =>
        {
            var issuedAt = await environment.GetCurrentTimeAsync();
            var request = new InvitationWorkflowRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "token-123",
                "child@example.com",
                issuedAt,
                issuedAt.AddDays(3));

            var handle = await environment.Client.StartWorkflowAsync(
                (IInvitationWorkflow wf) => wf.RunAsync(request),
                new(id: $"wf-{Guid.NewGuid():N}", taskQueue: taskQueue));

            await WaitForTimerAsync(handle, TimeSpan.FromDays(2));

            await environment.DelayAsync(TimeSpan.FromDays(2));
            await WaitForConditionAsync(() => activities.Reminders.Count == 1);

            var reminder = Assert.Single(activities.Reminders);
            Assert.Equal(request.InviteId, reminder.InviteId);
            Assert.Equal(request.Token, reminder.Token);
            Assert.Equal(request.ExpiresAt, reminder.ExpiresAt);
            Assert.Equal(request.IssuedAt, reminder.IssuedAt);
            Assert.Equal(request.Email, reminder.Email);

            await WaitForTimerAsync(handle, TimeSpan.FromDays(1));
            await environment.DelayAsync(TimeSpan.FromDays(1));
            await WaitForConditionAsync(() => activities.Expirations.Count == 1);

            var expiration = Assert.Single(activities.Expirations);
            Assert.Equal(request.GroupId, expiration.GroupId);
            Assert.Equal(request.InviteId, expiration.InviteId);
            Assert.Equal(request.ExpiresAt, expiration.ExpiredAt);

            await handle.GetResultAsync();
        }, cancellationToken);
    }

    [Fact]
    public async Task RunAsync_ShortValidity_SkipsReminderAndExpires()
    {
        var environment = await TryStartTimeSkippingAsync();
        if (environment is null)
        {
            return;
        }

        await using var environmentLifetime = environment;
        var activities = new RecordingInvitationActivities();
        var taskQueue = $"invitation-workflow-tests-{Guid.NewGuid():N}";

        using var worker = CreateWorker(environment, taskQueue, activities);

        var cancellationToken = TestContext.Current?.CancellationToken ?? CancellationToken.None;

        await worker.ExecuteAsync(async () =>
        {
            var issuedAt = await environment.GetCurrentTimeAsync();
            var request = new InvitationWorkflowRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "token-456",
                "friend@example.com",
                issuedAt,
                issuedAt.AddHours(12));

            var handle = await environment.Client.StartWorkflowAsync(
                (IInvitationWorkflow wf) => wf.RunAsync(request),
                new(id: $"wf-{Guid.NewGuid():N}", taskQueue: taskQueue));

            await WaitForTimerAsync(handle, TimeSpan.FromHours(12));

            await environment.DelayAsync(TimeSpan.FromDays(2));
            await WaitForConditionAsync(() => activities.Expirations.Count == 1);

            Assert.Empty(activities.Reminders);
            var expiration = Assert.Single(activities.Expirations);
            Assert.Equal(request.InviteId, expiration.InviteId);
            Assert.True(expiration.ExpiredAt >= request.ExpiresAt);

            await handle.GetResultAsync();
        }, cancellationToken);
    }

    private sealed class RecordingInvitationActivities : IInvitationActivities
    {
        public List<InviteEmailContext> Reminders { get; } = new();

        public List<ExpiredInvite> Expirations { get; } = new();

        public Task SendReminderAsync(InviteEmailContext context, CancellationToken cancellationToken = default)
        {
            Reminders.Add(context);
            return Task.CompletedTask;
        }

        public Task ExpireInviteAsync(Guid groupId, Guid inviteId, DateTimeOffset expiredAt, CancellationToken cancellationToken = default)
        {
            Expirations.Add(new ExpiredInvite(groupId, inviteId, expiredAt));
            return Task.CompletedTask;
        }
    }

    private sealed record ExpiredInvite(Guid GroupId, Guid InviteId, DateTimeOffset ExpiredAt);

    private static async Task WaitForConditionAsync(Func<bool> condition, TimeSpan? timeout = null, TimeSpan? pollInterval = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(50);
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < effectiveTimeout)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(interval);
        }

        throw new TimeoutException("Timed out waiting for condition to be satisfied.");
    }

    private static async Task WaitForTimerAsync(
        WorkflowHandle<IInvitationWorkflow> handle,
        TimeSpan expectedTimeout,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(10);
        var interval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        var deadline = DateTime.UtcNow + effectiveTimeout;

        while (DateTime.UtcNow < deadline)
        {
            var history = await handle.FetchHistoryAsync().ConfigureAwait(false);

            if (history.Events.Any(e => e.TimerStartedEventAttributes?.StartToFireTimeout.ToTimeSpan() == expectedTimeout))
            {
                return;
            }

            await Task.Delay(interval).ConfigureAwait(false);
        }

        throw new TimeoutException("Timed out waiting for workflow timer to start.");
    }

    private static TemporalWorker CreateWorker(
        WorkflowEnvironment environment,
        string taskQueue,
        RecordingInvitationActivities activities)
    {
        var options = new TemporalWorkerOptions(taskQueue)
            .AddWorkflow<InvitationWorkflow>();

        foreach (var definition in ActivityDefinition.CreateAll<IInvitationActivities>(activities))
        {
            options = options.AddActivity(definition);
        }

        return new TemporalWorker(environment.Client, options);
    }

    private static async Task<WorkflowEnvironment?> TryStartTimeSkippingAsync()
    {
        try
        {
            return await WorkflowEnvironment.StartTimeSkippingAsync();
        }
        catch (InvalidOperationException ex) when (IsDownloadFailure(ex))
        {
            TestContext.Current?.SendDiagnosticMessage(
                "Temporal test server is unavailable: {Message}",
                ex.Message);

            return null;
        }
    }

    private static bool IsDownloadFailure(Exception exception)
        => exception.Message.Contains("temporal-test-server", StringComparison.OrdinalIgnoreCase);
}

