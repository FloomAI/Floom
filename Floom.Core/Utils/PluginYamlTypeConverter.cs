using Floom.Pipeline.Entities.Dtos;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Floom.Utils;

public class PluginYamlTypeConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(PipelineDto.PluginConfigurationDto);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        var pluginConfig = new PipelineDto.PluginConfigurationDto();
        parser.Expect<MappingStart>();
        while (parser.Allow<MappingEnd>() == null)
        {
            var scalar = parser.Expect<Scalar>();
            var propertyName = scalar.Value;

            // Check what the next token is and handle accordingly
            if (parser.Accept<SequenceStart>(out _))
            {
                // If the next token is a SequenceStart, handle as a list
                parser.Expect<SequenceStart>();
                var list = new List<string>();
                while (!parser.Accept<SequenceEnd>(out _))
                {
                    var itemValue = parser.Expect<Scalar>().Value;
                    list.Add(itemValue);
                }
                parser.Expect<SequenceEnd>();

                // Here we handle storing the list, depending on your storage logic
                pluginConfig.Configuration[propertyName] = list;
            }
            else if (parser.Accept<Scalar>(out _))
            {
                // Handle scalar values as before
                var propertyValue = parser.Expect<Scalar>().Value;
                if (propertyName.Equals("package", StringComparison.OrdinalIgnoreCase))
                {
                    pluginConfig.Package = propertyValue;
                }
                else
                {
                    pluginConfig.Configuration[propertyName.ToLower()] = propertyValue;
                }
            }
            else if (parser.Accept<MappingStart>(out _))
            {
                // Handle nested mappings as before
                var nestedConfig = ReadNestedMapping(parser);
                pluginConfig.Configuration[propertyName] = nestedConfig;
            }
        }

        return pluginConfig;
    }

    private Dictionary<string, object> ReadNestedMapping(IParser parser)
    {
        // Implement or adjust nested mapping reading logic here
        var nestedConfig = new Dictionary<string, object>();
        parser.Expect<MappingStart>();
        while (parser.Allow<MappingEnd>() == null)
        {
            var scalar = parser.Expect<Scalar>();
            var propertyName = scalar.Value;
            // Here you might need to also handle sequences or nested mappings depending on your needs
            var propertyValue = parser.Expect<Scalar>().Value;
            nestedConfig[propertyName] = propertyValue;
        }
        return nestedConfig;
    }

    
    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        // Implement serialization logic if needed
    }
}