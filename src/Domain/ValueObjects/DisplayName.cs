using System.Text;
using System.Text.RegularExpressions;

namespace FotoTime.Domain.ValueObjects;

public sealed class DisplayName : IEquatable<DisplayName>
{
    private static readonly Regex VisibleCharactersRegex = new("\\P{C}", RegexOptions.Compiled);
    private DisplayName(string value) => Value = value;

    public string Value { get; }

    public static DisplayName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Display name is required.", nameof(value));
        }

        var trimmed = value.Trim();
        if (trimmed.Length < 3 || trimmed.Length > 50)
        {
            throw new ArgumentException("Display name must be between 3 and 50 characters.", nameof(value));
        }

        if (!IsVisible(trimmed))
        {
            throw new ArgumentException("Display name contains invalid characters.", nameof(value));
        }

        if (ContainsDisallowedEmoji(trimmed))
        {
            throw new ArgumentException("Display name contains disallowed emoji characters.", nameof(value));
        }

        return new DisplayName(trimmed.Normalize());
    }

    private static bool IsVisible(string value)
    {
        if (value.Any(char.IsControl))
        {
            return false;
        }

        return VisibleCharactersRegex.IsMatch(value);
    }

    private static bool ContainsDisallowedEmoji(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (rune.Value is >= 0x1F300 and <= 0x1FAFF)
            {
                return true;
            }
        }

        return false;
    }

    public override string ToString() => Value;

    public bool Equals(DisplayName? other) => other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => obj is DisplayName other && Equals(other);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
}
