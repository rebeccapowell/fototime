using FotoTime.Domain.Groups;

namespace FotoTime.Application.Groups;

public interface IGroupRepository
{
    public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default);

    public Task SaveAsync(Group group, CancellationToken cancellationToken = default);
}
