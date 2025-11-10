namespace FotoTime.Application.Common;

public interface IClock
{
    public DateTimeOffset UtcNow { get; }
}
