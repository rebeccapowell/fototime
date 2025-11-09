using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FotoTime.Domain.ValueObjects;

public sealed class Slug : IEquatable<Slug>
{
    private static readonly Regex ValidSlug = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);
    private Slug(string value) => Value = value;

    public string Value { get; }

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug is required.", nameof(value));
        }

        var normalized = Normalize(value);
        if (normalized.Length < 3 || normalized.Length > 40)
        {
            throw new ArgumentException("Slug must be between 3 and 40 characters.", nameof(value));
        }

        if (!ValidSlug.IsMatch(normalized))
        {
            throw new ArgumentException("Slug must contain lowercase letters or digits separated by single hyphens.", nameof(value));
        }

        return new Slug(normalized);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        var lastWasHyphen = false;

        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsWhiteSpace(ch) || ch is '_' or '-')
            {
                if (!lastWasHyphen && builder.Length > 0)
                {
                    builder.Append('-');
                    lastWasHyphen = true;
                }

                continue;
            }

            if (char.IsLetterOrDigit(ch))
            {
                var ascii = RemoveDiacritics(ch);
                if (ascii <= 0x7F)
                {
                    builder.Append(char.ToLowerInvariant((char)ascii));
                    lastWasHyphen = false;
                }
            }
        }

        var normalized = builder.ToString().Trim('-');
        while (normalized.Contains("--", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("--", "-", StringComparison.Ordinal);
        }

        return normalized;
    }

    private static int RemoveDiacritics(char ch)
    {
        var formD = ch.ToString().Normalize(NormalizationForm.FormD);
        foreach (var c in formD)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                return c;
            }
        }

        return ch;
    }

    public override string ToString() => Value;

    public bool Equals(Slug? other) => other is not null && Value.Equals(other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => obj is Slug other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
}
