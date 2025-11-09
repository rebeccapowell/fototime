using Temporalio.Activities;

namespace Infrastructure.Temporal.Activities;

public interface IPingActivity
{
    Task<string> PingAsync();
}

public class PingActivity : IPingActivity
{
    [Activity]
    public async Task<string> PingAsync()
    {
        return await Task.FromResult("Pong @ " + DateTime.UtcNow);
    }
}