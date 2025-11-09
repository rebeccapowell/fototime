using System.Text;
using FotoTime.Domain.Common;

namespace FotoTime.Domain.Comments;

public sealed class Comment : Entity
{
    private Comment(Guid id, Guid photoId, Guid weeklyTopicId, Guid membershipId, string text, DateTimeOffset createdAt) : base(id)
    {
        Guard.AgainstEmpty(photoId, nameof(photoId));
        Guard.AgainstEmpty(weeklyTopicId, nameof(weeklyTopicId));
        Guard.AgainstEmpty(membershipId, nameof(membershipId));

        PhotoId = photoId;
        WeeklyTopicId = weeklyTopicId;
        MembershipId = membershipId;
        Text = Sanitize(text);
        CreatedAt = createdAt;
        IsDeleted = false;
    }

    public Guid PhotoId { get; }

    public Guid WeeklyTopicId { get; }

    public Guid MembershipId { get; }

    public string Text { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public static Comment Create(Guid id, Guid photoId, Guid weeklyTopicId, Guid membershipId, string text, DateTimeOffset createdAt)
        => new(id, photoId, weeklyTopicId, membershipId, text, createdAt);

    public void SoftDelete(DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAt = deletedAt;
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Comments must include content.", nameof(value));
        }

        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Trim())
        {
            if (!char.IsControl(ch) || ch == '\n')
            {
                builder.Append(ch);
            }
        }

        if (builder.Length == 0)
        {
            throw new ArgumentException("Comments must include visible content.", nameof(value));
        }

        return builder.ToString();
    }
}
