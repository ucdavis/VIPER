using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Viper.Areas.Reports.Engine;

/// <summary>
/// Binds a request's JSON body to a report's parameter class. Unknown properties are rejected
/// rather than ignored, so a misspelled parameter can't silently fall back to its default.
/// </summary>
internal static class ReportParameterBinder
{
    public const string NotAnObjectMessage = "Report parameters must be a JSON object.";
    public const string UnreadableMessage = "A report parameter is not recognized or has a value of the wrong type.";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    private static readonly JsonElement EmptyObject = ParseEmptyObject();

    public static bool TryBind(
        Type parametersType,
        JsonElement? json,
        [NotNullWhen(true)] out object? parameters,
        [NotNullWhen(false)] out ReportValidationError? error)
    {
        ArgumentNullException.ThrowIfNull(parametersType);
        parameters = null;
        error = null;

        JsonElement source = json ?? EmptyObject;
        if (source.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            source = EmptyObject;
        }

        if (source.ValueKind != JsonValueKind.Object)
        {
            error = new ReportValidationError(null, NotAnObjectMessage);
            return false;
        }

        try
        {
            parameters = source.Deserialize(parametersType, JsonOptions)
                ?? throw new JsonException("Report parameters deserialized to null.");
            return true;
        }
        catch (JsonException ex)
        {
            error = new ReportValidationError(ParameterName(ex.Path), UnreadableMessage);
            return false;
        }
    }

    private static JsonElement ParseEmptyObject()
    {
        using JsonDocument document = JsonDocument.Parse("{}");
        return document.RootElement.Clone();
    }

    /// <summary>
    /// Turns a JSON path such as <c>$.departments[1]</c> into the parameter name <c>departments</c>.
    /// </summary>
    public static string? ParameterName(string? path)
    {
        if (path is null || !path.StartsWith("$.", StringComparison.Ordinal))
        {
            return null;
        }

        string name = path[2..].Split('[', '.')[0];
        return name.Length > 0 ? name : null;
    }
}
