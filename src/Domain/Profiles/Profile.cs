using FotoTime.Domain.Common;
using FotoTime.Domain.ValueObjects;

namespace FotoTime.Domain.Profiles;

public sealed class Profile : Entity
{
    private string? _avatarUrl;
    private string? _bio;

    public Profile(Guid id, Guid userId, DisplayName displayName, ContentSafetyTag preferredTag) : base(id)
    {
        Guard.AgainstEmpty(userId, nameof(userId));
        Guard.AgainstNull(displayName, nameof(displayName));

        UserId = userId;
        DisplayName = displayName;
        PreferredContentSafety = preferredTag;
    }

    public Guid UserId { get; }

    public DisplayName DisplayName { get; private set; }

    public string? AvatarUrl
    {
        get => _avatarUrl;
        private set
        {
            if (value is not null && value.Length > 2048)
            {
                throw new ArgumentException("Avatar URLs must be 2048 characters or fewer.", nameof(value));
            }

            _avatarUrl = value;
        }
    }

    public string? Bio
    {
        get => _bio;
        private set
        {
            if (value is not null && value.Length > 500)
            {
                throw new ArgumentException("Bio must be 500 characters or fewer.", nameof(value));
            }

            _bio = value;
        }
    }

    public bool AllowEmailNotifications { get; private set; }

    public bool HideProfileFromSearch { get; private set; }

    public ContentSafetyTag PreferredContentSafety { get; private set; }

    public void UpdateDisplayName(DisplayName displayName)
    {
        Guard.AgainstNull(displayName, nameof(displayName));
        DisplayName = displayName;
    }

    public void UpdateAvatar(string? avatarUrl) => AvatarUrl = avatarUrl;

    public void UpdateBio(string? bio)
    {
        if (bio is not null)
        {
            bio = bio.Trim();
            if (bio.Length == 0)
            {
                bio = null;
            }
        }

        Bio = bio;
    }

    public void SetNotificationPreferences(bool allowEmails, bool hideFromSearch)
    {
        AllowEmailNotifications = allowEmails;
        HideProfileFromSearch = hideFromSearch;
    }

    public void ChangePreferredContentSafety(ContentSafetyTag next, bool hasModeratorApproval)
    {
        PreferredContentSafety.EnsureTransition(next, hasModeratorApproval);
        PreferredContentSafety = next;
    }
}
