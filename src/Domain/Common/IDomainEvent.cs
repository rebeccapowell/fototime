namespace FotoTime.Domain.Common;

public interface IDomainEvent
{
    public DateTimeOffset OccurredOn { get; }
}
