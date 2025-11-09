namespace FotoTime.Domain.ValueObjects;

public enum ContentSafetyTag
{
    Safe = 0,
    Mature = 1,
    Restricted = 2
}

public static class ContentSafetyTagExtensions
{
    public static void EnsureTransition(this ContentSafetyTag current, ContentSafetyTag next, bool hasModeratorApproval)
    {
        if (current == next)
        {
            return;
        }

        if (next > current)
        {
            return;
        }

        if (!hasModeratorApproval)
        {
            throw new InvalidOperationException("Downgrading content safety requires moderator approval.");
        }
    }
}
