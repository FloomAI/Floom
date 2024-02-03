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

            switch (parser.Current)
            {
                case Scalar:
                {
                    var propertyValue = parser.Expect<Scalar>().Value;

                    if (propertyName.Equals("package", StringComparison.OrdinalIgnoreCase))
                    {
                        pluginConfig.Package = propertyValue;
                    }
                    else
                    {
                        pluginConfig.Configuration[propertyName.ToLower()] = propertyValue;
                    }

                    break;
                }
                case MappingStart:
                {
                    var nestedConfig = ReadNestedMapping(parser);
                    pluginConfig.Configuration[propertyName] = nestedConfig;
                    break;
                }
            }
        }

        return pluginConfig;
    }

    private Dictionary<string, object> ReadNestedMapping(IParser parser)
    {
        var nestedConfig = new Dictionary<string, object>();
        parser.Expect<MappingStart>();
        while (parser.Allow<MappingEnd>() == null)
        {
            var key = parser.Expect<Scalar>().Value;
            var value = parser.Expect<Scalar>().Value;
            nestedConfig[key] = value;
        }

        return nestedConfig;
    }


    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        // Implement serialization logic if needed
    }
}