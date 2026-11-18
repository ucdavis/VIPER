using System.Text.Json;
using Viper.Areas.ReportBuilder.Models;

namespace Viper.Areas.ReportBuilder.Services;

/// <summary>Reads a parameter's fixed choice list (<c>choicesList</c>), stored as JSON.</summary>
public static class ReportChoiceList
{
    /// <summary>
    /// The choices in a JSON array whose items are strings (value and label alike) or objects
    /// with <c>value</c> and <c>label</c>. Null when the JSON isn't such an array.
    /// </summary>
    public static IReadOnlyList<ReportChoice>? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            var choices = new List<ReportChoice>();
            foreach (JsonElement item in document.RootElement.EnumerateArray())
            {
                ReportChoice? choice = ToChoice(item);
                if (choice is null)
                {
                    return null;
                }

                choices.Add(choice);
            }

            return choices;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ReportChoice? ToChoice(JsonElement item)
    {
        if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } text)
        {
            return new ReportChoice { Value = text, Label = text };
        }

        if (item.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        string? value = Text(item, "value");
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        string? label = Text(item, "label");
        return new ReportChoice { Value = value, Label = string.IsNullOrWhiteSpace(label) ? value : label };
    }

    private static string? Text(JsonElement item, string name)
    {
        return item.EnumerateObject()
            .Where(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            .Select(p => p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString() : p.Value.ToString())
            .FirstOrDefault();
    }
}
