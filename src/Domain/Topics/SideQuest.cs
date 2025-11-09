using FotoTime.Domain.Common;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Topics;

public sealed class SideQuest : Entity
{
    public SideQuest(Guid id, Guid weeklyTopicId, Slug slug, string title) : base(id)
    {
        Guard.AgainstEmpty(weeklyTopicId, nameof(weeklyTopicId));
        Guard.AgainstNull(slug, nameof(slug));
        Guard.AgainstNullOrWhiteSpace(title, nameof(title));

        WeeklyTopicId = weeklyTopicId;
        Slug = slug;
        Title = title.Trim();
    }

    public Guid WeeklyTopicId { get; }

    public Slug Slug { get; }

    public string Title { get; }
}
