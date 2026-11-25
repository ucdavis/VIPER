using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Viper.Areas.Personnel.Models.Eis;

/// <summary>
/// Turns the HTML that MyInfoVault stores into plain text, so the page never renders HTML it
/// didn't write. The legacy page stripped tags from memberships but output boards, honors and
/// the focus areas as raw HTML.
/// </summary>
public static partial class EisHtmlText
{
    private static readonly string[] ItemTags = ["p", "li"];

    /// <summary>The text of <paramref name="html"/>, entities decoded and whitespace collapsed; null when blank.</summary>
    public static string? ToText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        return Clean(document.DocumentNode.InnerText);
    }

    /// <summary>
    /// The paragraphs or list items of <paramref name="html"/> as separate texts, or the whole
    /// text as one item when it has neither. The legacy page turned each paragraph into a bullet.
    /// </summary>
    public static IReadOnlyList<string> ToItems(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        var document = new HtmlDocument();
        document.LoadHtml(html);
        List<string> items = document.DocumentNode
            .Descendants()
            .Where(node => ItemTags.Contains(node.Name, StringComparer.OrdinalIgnoreCase)
                && !node.Ancestors().Any(ancestor => ItemTags.Contains(ancestor.Name, StringComparer.OrdinalIgnoreCase)))
            .Select(node => Clean(node.InnerText))
            .OfType<string>()
            .ToList();
        if (items.Count > 0)
        {
            return items;
        }

        string? text = Clean(document.DocumentNode.InnerText);
        return text is null ? [] : [text];
    }

    private static string? Clean(string text)
    {
        string cleaned = Whitespace().Replace(WebUtility.HtmlDecode(text), " ").Trim();
        return cleaned.Length > 0 ? cleaned : null;
    }

    [GeneratedRegex(@"\s+", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex Whitespace();
}
