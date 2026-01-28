using HtmlAgilityPack;

namespace Viper.Services;

/// <summary>
/// Converts HTML email content to readable plaintext for multipart emails.
///
/// Conventions:
/// - class="data-table" on a table = format as pipe-separated table
/// - class="section" on any element = add blank line before in plaintext
/// </summary>
public static class HtmlToTextConverter
{
    private const string DataTableClass = "data-table";
    private const string SectionClass = "section";
    private const string SectionMarker = "\n\n§§§\n"; // Temporary marker for section breaks

    /// <summary>
    /// Converts HTML to plaintext, preserving readability.
    /// Links are formatted as "text (url)" for visibility in text clients.
    /// Tables with class="data-table" are formatted with pipe separators.
    /// Elements with class="section" get a blank line before them.
    /// </summary>
    public static string Convert(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Remove script, style, and head nodes entirely
        RemoveNodes(doc, "//script|//style|//head");

        // Mark section boundaries before other processing
        MarkSections(doc);

        // Convert links to "linkText (url)" format
        ConvertLinks(doc);

        // Convert data tables to pipe-separated format
        ConvertDataTables(doc);

        // Remove all remaining table tags but keep content
        UnwrapTables(doc);

        // Handle list items with bullets
        AddListBullets(doc);

        // Add newlines after block elements
        AddBlockNewlines(doc);

        // Extract text and clean up
        return CleanupText(doc);
    }

    private static void RemoveNodes(HtmlDocument doc, string xpath)
    {
        var nodes = doc.DocumentNode.SelectNodes(xpath);
        if (nodes == null) return;

        foreach (var node in nodes.ToList())
        {
            node.Remove();
        }
    }

    private static void MarkSections(HtmlDocument doc)
    {
        // Find elements with class="section" and insert a marker before them
        var sections = doc.DocumentNode.SelectNodes("//*[contains(@class, '" + SectionClass + "')]");
        if (sections == null) return;

        foreach (var section in sections.ToList())
        {
            var marker = doc.CreateTextNode(SectionMarker);
            section.PrependChild(marker);
        }
    }

    private static void ConvertLinks(HtmlDocument doc)
    {
        var links = doc.DocumentNode.SelectNodes("//a[@href]");
        if (links == null) return;

        foreach (var link in links.ToList())
        {
            var href = link.GetAttributeValue("href", "");
            var linkText = link.InnerText.Trim();

            if (string.IsNullOrEmpty(href) || string.IsNullOrEmpty(linkText))
                continue;

            // Don't duplicate if the link text is already the URL
            if (linkText.Equals(href, StringComparison.OrdinalIgnoreCase) ||
                linkText.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var replacement = doc.CreateTextNode($"{linkText} ({href})");
            link.ParentNode?.ReplaceChild(replacement, link);
        }
    }

    private static void ConvertDataTables(HtmlDocument doc)
    {
        var tables = doc.DocumentNode.SelectNodes("//table[@class]");
        if (tables == null) return;

        foreach (var table in tables.ToList())
        {
            var tableClass = table.GetAttributeValue("class", "");

            // Only process tables with data-table class
            if (!tableClass.Contains(DataTableClass, StringComparison.OrdinalIgnoreCase))
                continue;

            // Check if table also has section class (needs blank line before)
            var hasSection = tableClass.Contains(SectionClass, StringComparison.OrdinalIgnoreCase);

            var rows = table.SelectNodes(".//tr");
            if (rows == null) continue;

            var tableLines = new List<string>();

            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td|.//th");
                if (cells == null) continue;

                var cellTexts = cells
                    .Select(cell => NormalizeWhitespace(cell.InnerText))
                    .ToList();

                if (cellTexts.All(string.IsNullOrWhiteSpace))
                    continue;

                // Single cell rows (colspan) are sub-items
                if (cellTexts.Count == 1)
                {
                    if (!string.IsNullOrWhiteSpace(cellTexts[0]))
                    {
                        tableLines.Add(cellTexts[0]);
                    }
                }
                else
                {
                    tableLines.Add(string.Join(" | ", cellTexts));
                }
            }

            if (tableLines.Count > 0)
            {
                // Include section marker if table has section class
                var prefix = hasSection ? SectionMarker : "\n";
                var tableText = prefix + string.Join("\n", tableLines) + "\n";
                var replacement = doc.CreateTextNode(tableText);
                table.ParentNode?.ReplaceChild(replacement, table);
            }
        }
    }

    private static void UnwrapTables(HtmlDocument doc)
    {
        // Remove table structure but keep text content
        var tableElements = doc.DocumentNode.SelectNodes("//table|//tr|//td|//th|//tbody|//thead");
        if (tableElements == null) return;

        foreach (var element in tableElements.ToList())
        {
            var parent = element.ParentNode;
            if (parent == null) continue;

            foreach (var child in element.ChildNodes.ToList())
            {
                parent.InsertBefore(child, element);
            }
            element.Remove();
        }
    }

    private static void AddListBullets(HtmlDocument doc)
    {
        var listItems = doc.DocumentNode.SelectNodes("//li");
        if (listItems == null) return;

        foreach (var li in listItems)
        {
            var bullet = doc.CreateTextNode("• ");
            li.PrependChild(bullet);
        }
    }

    private static void AddBlockNewlines(HtmlDocument doc)
    {
        var blockTags = new[] { "p", "div", "br", "li", "h1", "h2", "h3", "h4", "h5", "h6" };

        var allNodes = blockTags
            .Select(tag => doc.DocumentNode.SelectNodes($"//{tag}"))
            .Where(nodes => nodes != null)
            .SelectMany(nodes => nodes);

        foreach (var node in allNodes)
        {
            node.AppendChild(doc.CreateTextNode("\n"));
        }
    }

    private static string CleanupText(HtmlDocument doc)
    {
        var text = System.Net.WebUtility.HtmlDecode(doc.DocumentNode.InnerText);

        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .ToList();

        // Process lines: remove empty lines but preserve section markers
        var result = new List<string>();

        foreach (var line in lines)
        {
            if (line == "§§§")
            {
                // Section marker = add blank line (but not at the start)
                if (result.Count > 0 && result[^1] != "")
                {
                    result.Add("");
                }
            }
            else if (!string.IsNullOrEmpty(line))
            {
                result.Add(line);
            }
        }

        return string.Join("\n", result).Trim();
    }

    private static string NormalizeWhitespace(string text)
    {
        var result = text.Trim().Replace("\r", " ").Replace("\n", " ");
        while (result.Contains("  "))
            result = result.Replace("  ", " ");
        return result;
    }
}
