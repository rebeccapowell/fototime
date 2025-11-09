using Temporalio.Activities;

namespace Infrastructure.Temporal.Activities;

public interface IPingActivity
{
    Task<DateTime> PingAsync();
}

public class PingActivity : IPingActivity
{
    [Activity]
    public Task<DateTime> PingAsync()
    {
        return Task.FromResult(DateTime.UtcNow);
    }
}