using System.Text.RegularExpressions;
using FotoTime.Domain.Common;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Groups;

public sealed class Invite : Entity
{
    private static readonly Regex EmailRegex = new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled);

    private Invite(
        Guid id,
        Guid groupId,
        Guid inviterMembershipId,
        string token,
        string email,
        Period validFor,
        DateTimeOffset createdAt) : base(id)
    {
        Guard.AgainstEmpty(groupId, nameof(groupId));
        Guard.AgainstEmpty(inviterMembershipId, nameof(inviterMembershipId));
        Guard.AgainstNullOrWhiteSpace(token, nameof(token));
        Guard.AgainstNullOrWhiteSpace(email, nameof(email));

        if (!EmailRegex.IsMatch(email))
        {
            throw new ArgumentException("Invite email must be valid.", nameof(email));
        }

        GroupId = groupId;
        InviterMembershipId = inviterMembershipId;
        Token = token;
        Email = email;
        ValidFor = validFor;
        CreatedAt = createdAt;
        Status = InviteStatus.Pending;
    }

    public Guid GroupId { get; }

    public Guid InviterMembershipId { get; }

    public string Token { get; }

    public string Email { get; }

    public Period ValidFor { get; }

    public DateTimeOffset CreatedAt { get; }

    public InviteStatus Status { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static Invite Create(
        Guid id,
        Guid groupId,
        Guid inviterMembershipId,
        string token,
        string email,
        Period validFor,
        DateTimeOffset createdAt) => new(id, groupId, inviterMembershipId, token, email, validFor, createdAt);

    public void Accept(DateTimeOffset acceptedAt)
    {
        if (Status != InviteStatus.Pending)
        {
            throw new InvalidOperationException("Invite has already been completed.");
        }

        if (!ValidFor.Contains(acceptedAt))
        {
            throw new InvalidOperationException("Invite can only be accepted within its validity period.");
        }

        Status = InviteStatus.Accepted;
        CompletedAt = acceptedAt;
    }

    public void Expire(DateTimeOffset expiredAt)
    {
        if (Status != InviteStatus.Pending)
        {
            return;
        }

        if (expiredAt < ValidFor.End)
        {
            throw new InvalidOperationException("An invite may only expire after its period ends.");
        }

        Status = InviteStatus.Expired;
        CompletedAt = expiredAt;
    }
}

public enum InviteStatus
{
    Pending,
    Accepted,
    Expired
}
