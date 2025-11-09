using System.Collections.Concurrent;
using FotoTime.Application.Groups;
using FotoTime.Domain.Groups;

namespace Infrastructure.Groups;

public sealed class InMemoryGroupRepository : IGroupRepository
{
    private readonly ConcurrentDictionary<Guid, Group> _groups = new();

    public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        _groups.TryGetValue(groupId, out var group);
        return Task.FromResult(group);
    }

    public Task SaveAsync(Group group, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(group);
        _groups[group.Id] = group;
        return Task.CompletedTask;
    }
}
