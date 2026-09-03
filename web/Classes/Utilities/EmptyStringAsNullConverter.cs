using System.Text.Json;
using System.Text.Json.Serialization;

namespace Viper.Classes.Utilities;

/// <summary>
/// Binds an empty string as null for a nullable value type, most usefully a date.
/// A cleared date input posts "", which System.Text.Json otherwise rejects for
/// DateTime?/DateOnly?, failing model binding with a 400 before the action runs.
/// Apply per property with [JsonConverter(typeof(EmptyStringAsNullConverter&lt;DateTime&gt;))].
/// </summary>
public class EmptyStringAsNullConverter<T> : JsonConverter<T?> where T : struct
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        if (reader.TokenType == JsonTokenType.String && string.IsNullOrWhiteSpace(reader.GetString()))
        {
            return null;
        }
        // T, not T?, so this resolves to the built-in converter rather than recursing.
        return JsonSerializer.Deserialize<T>(ref reader, options);
    }

    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            JsonSerializer.Serialize(writer, value.Value, options);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}
