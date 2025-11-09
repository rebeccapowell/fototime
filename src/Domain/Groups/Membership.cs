using FotoTime.Domain.Common;
namespace FotoTime.Domain.Groups;

public sealed class Membership : Entity
{
    public Membership(Guid id, Guid groupId, Guid userId, MembershipRole role) : base(id)
    {
        Guard.AgainstEmpty(groupId, nameof(groupId));
        Guard.AgainstEmpty(userId, nameof(userId));

        GroupId = groupId;
        UserId = userId;
        Role = role;
    }

    public Guid GroupId { get; }

    public Guid UserId { get; }

    public MembershipRole Role { get; private set; }

    public MembershipState State { get; private set; } = MembershipState.Active;

    public void Suspend()
    {
        if (State == MembershipState.Suspended)
        {
            return;
        }

        State = MembershipState.Suspended;
    }

    public void Reinstate()
    {
        if (State == MembershipState.Active)
        {
            return;
        }

        State = MembershipState.Active;
    }

    public void PromoteToOwner()
    {
        Role = MembershipRole.Owner;
    }
}

public enum MembershipRole
{
    Owner,
    Member
}

public enum MembershipState
{
    Active,
    Suspended
}
