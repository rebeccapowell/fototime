using Temporalio.Testing;
using Temporalio.Worker;

namespace Unit.Infrastructure.Temporal;

internal static class WorkflowEnvironmentExtensions
{
    public static async ValueTask<IAsyncDisposable> StartWorkerAsync(
        this WorkflowEnvironment environment,
        TemporalWorkerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(options);

        var worker = new TemporalWorker(environment.Client, options);
        var stopSignal = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workerTask = worker.ExecuteAsync(async () =>
        {
            await stopSignal.Task.ConfigureAwait(false);
        }, cancellationToken);

        // Yield control back to caller to give the worker a chance to start polling before workflow execution.
        await Task.Yield();

        return new WorkerHandle(worker, workerTask, stopSignal);
    }

    private sealed class WorkerHandle : IAsyncDisposable
    {
        private readonly TemporalWorker worker;
        private readonly Task workerTask;
        private readonly TaskCompletionSource<object?> stopSignal;

        public WorkerHandle(TemporalWorker worker, Task workerTask, TaskCompletionSource<object?> stopSignal)
        {
            this.worker = worker;
            this.workerTask = workerTask;
            this.stopSignal = stopSignal;
        }

        public async ValueTask DisposeAsync()
        {
            stopSignal.TrySetResult(null);

            try
            {
                await workerTask.ConfigureAwait(false);
            }
            finally
            {
                worker.Dispose();
            }
        }
    }
}
