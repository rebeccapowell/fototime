using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace FotoTime.Domain.Common;

internal static class Guard
{
    public static void AgainstNull<T>([NotNull] T? value, string name)
    {
        if (value is null)
        {
            throw new ArgumentNullException(name);
        }
    }

    public static void AgainstEmpty(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException($"{name} must not be empty.", name);
        }
    }

    public static void AgainstNullOrWhiteSpace(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{name} must not be empty.", name);
        }
    }

    public static void AgainstNegativeOrZero(int value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than zero.");
        }
    }

    public static void AgainstNegativeOrZero(long value, string name)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must be greater than zero.");
        }
    }

    public static void AgainstExceeding(long value, long maximum, string name)
    {
        if (value > maximum)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must not exceed {maximum.ToString(CultureInfo.InvariantCulture)}.");
        }
    }

    public static void AgainstExceeding(int value, int maximum, string name)
    {
        if (value > maximum)
        {
            throw new ArgumentOutOfRangeException(name, value, $"{name} must not exceed {maximum}.");
        }
    }
}
