using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FSH.Modules.Auditing.Contracts;

/// <summary>
/// Forces an enum to serialize as its underlying integer even when a global
/// <see cref="JsonStringEnumConverter"/> is registered. Applied to <c>[Flags]</c>
/// enums (<see cref="AuditTag"/>, <see cref="BodyCapture"/>) — a bitset is not a
/// single named value, and the converter's comma-joined string form (e.g.
/// "PiiMasked, Sampled") breaks bitwise consumers. Reading accepts an integer or,
/// defensively, a comma/space-delimited list of member names.
/// </summary>
public sealed class NumericEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return (TEnum)Enum.ToObject(typeof(TEnum), reader.GetInt64());
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            return string.IsNullOrWhiteSpace(raw)
                ? default
                : Enum.Parse<TEnum>(raw, ignoreCase: true);
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when reading {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
    }
}
