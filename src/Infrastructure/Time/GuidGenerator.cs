using FotoTime.Application.Common;

namespace Infrastructure.Time;

public sealed class GuidGenerator : IIdGenerator
{
    public Guid NewGuid() => Guid.NewGuid();
}
