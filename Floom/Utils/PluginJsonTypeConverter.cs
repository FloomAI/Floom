using System.Text.Json;
using System.Text.Json.Serialization;
using Floom.Pipeline.Entities.Dtos;

namespace Floom.Utils;

public class PluginConfigurationJsonConverter : JsonConverter<PipelineDto.PluginConfigurationDto>
{
    public override PipelineDto.PluginConfigurationDto Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        var pluginConfig = new PipelineDto.PluginConfigurationDto();

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.EndObject:
                    return pluginConfig;
                case JsonTokenType.PropertyName:
                {
                    var propertyName = reader.GetString();
                    reader.Read();
                    if (propertyName.Equals("package", StringComparison.OrdinalIgnoreCase))
                    {
                        pluginConfig.Package = reader.GetString();
                    }
                    else
                    {
                        pluginConfig.Configuration[propertyName.ToLower()] = ReadValue(ref reader, options);
                    }

                    break;
                }
                case JsonTokenType.None:
                case JsonTokenType.StartObject:
                case JsonTokenType.StartArray:
                case JsonTokenType.EndArray:
                case JsonTokenType.Comment:
                case JsonTokenType.String:
                case JsonTokenType.Number:
                case JsonTokenType.True:
                case JsonTokenType.False:
                case JsonTokenType.Null:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        throw new JsonException("Expected EndObject token");
    }


    private object ReadValue(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString() ?? string.Empty;
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var l))
                {
                    return l;
                }

                return reader.GetDouble();
            case JsonTokenType.True:
            case JsonTokenType.False:
                return reader.GetBoolean();
            case JsonTokenType.StartObject:
                return ReadDictionary(ref reader, options);
            case JsonTokenType.StartArray:
                return ReadList(ref reader, options);
            case JsonTokenType.None:
            case JsonTokenType.EndObject:
            case JsonTokenType.EndArray:
            case JsonTokenType.PropertyName:
            case JsonTokenType.Comment:
            case JsonTokenType.Null:
            default:
                throw new JsonException("Unsupported JSON token");
        }
    }


    private Dictionary<string, object> ReadDictionary(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var dictionary =
            new Dictionary<string, object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return dictionary;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var key = reader.GetString();
                reader.Read();
                var value = ReadValue(ref reader, options);
                dictionary[key] = value;
            }
        }

        throw new JsonException("Invalid JSON structure in dictionary");
    }


    private List<object> ReadList(ref Utf8JsonReader reader, JsonSerializerOptions options)
    {
        var list = new List<object>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return list;
            }

            var value = ReadValue(ref reader, options);
            list.Add(value);
        }

        throw new JsonException("Invalid JSON structure in list");
    }

    public override void Write(Utf8JsonWriter writer, PipelineDto.PluginConfigurationDto value,
        JsonSerializerOptions options)
    {
        // Implement serialization logic if needed
    }
}