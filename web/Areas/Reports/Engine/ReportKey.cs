namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Report keys look like <c>personnel.employees-on-leave</c>: a lowercase area segment, one dot,
/// then a kebab-case name. Keys appear in URLs and export filenames, so the character set is
/// deliberately narrow.
/// </summary>
public static class ReportKey
{
    public static bool IsValid(string? key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        int dot = key.IndexOf('.', StringComparison.Ordinal);
        return dot > 0
            && dot < key.Length - 1
            && !key.AsSpan(dot + 1).Contains('.')
            && IsSegment(key.AsSpan(0, dot))
            && IsSegment(key.AsSpan(dot + 1));
    }

    private static bool IsSegment(ReadOnlySpan<char> segment)
    {
        if (segment[0] == '-' || segment[^1] == '-')
        {
            return false;
        }

        foreach (char c in segment)
        {
            if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-')
            {
                return false;
            }
        }

        return true;
    }
}
