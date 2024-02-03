using System.Text.Json;

namespace Floom.Utils;

using System.Text.Json.Serialization;

public class StringEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (Enum.TryParse<TEnum>(value, true, out var result))
        {
            return result;
        }

        throw new JsonException($"{value} is not supported");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}