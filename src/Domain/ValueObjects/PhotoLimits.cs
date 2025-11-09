namespace FotoTime.Domain.ValueObjects;

public sealed record PhotoLimits(int MaxSubmissionsPerMember, long MaxFileSizeBytes, int MaxLongEdgePixels)
{
    public const long MaximumFileSizeBytes = 10 * 1024 * 1024;
    public const int MaximumResolutionPixels = 7680;

    public static PhotoLimits Create(int maxSubmissionsPerMember, long maxFileSizeBytes, int maxLongEdgePixels)
    {
        if (maxSubmissionsPerMember <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSubmissionsPerMember), "Members must be able to submit at least one photo.");
        }

        if (maxFileSizeBytes <= 0 || maxFileSizeBytes > MaximumFileSizeBytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maxFileSizeBytes), $"File size must be between 1 and {MaximumFileSizeBytes} bytes.");
        }

        if (maxLongEdgePixels <= 0 || maxLongEdgePixels > MaximumResolutionPixels)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLongEdgePixels), $"Resolution must be between 1 and {MaximumResolutionPixels} pixels.");
        }

        return new PhotoLimits(maxSubmissionsPerMember, maxFileSizeBytes, maxLongEdgePixels);
    }

    public static PhotoLimits Default { get; } = Create(1, MaximumFileSizeBytes, MaximumResolutionPixels);
}
