using Temporalio.Activities;

#pragma warning disable IDE0040
namespace Infrastructure.Temporal.Activities
{
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
}
#pragma warning restore IDE0040
