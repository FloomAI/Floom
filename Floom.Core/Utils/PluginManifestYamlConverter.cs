using Floom.Plugin;
using Floom.Plugin.Manifest;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace Floom.Utils;

public class PluginManifestYamlConverter : IYamlTypeConverter
{
    public bool Accepts(Type type)
    {
        return type == typeof(PluginManifestEntity.PluginManifestEntityParameter);
    }

    public object ReadYaml(IParser parser, Type type)
    {
        var parameter = new PluginManifestEntity.PluginManifestEntityParameter();
        parser.Consume<MappingStart>();

        while (!parser.TryConsume<MappingEnd>(out _))
        {
            var propertyName = parser.Consume<Scalar>().Value;
            switch (propertyName)
            {
                case "type":
                    parameter.type = parser.Consume<Scalar>().Value;
                    break;
                case "description":
                    parameter.description = parser.Consume<Scalar>().Value;
                    break;
                case "default":
                    parameter.defaultValue = ReadDefaultValue(parser);
                    break;
                default:
                    parser.SkipThisAndNestedEvents();
                    break;
            }
        }

        return parameter;
    }

    private Dictionary<object, object> ReadDefaultValue(IParser parser)
    {
        var defaultValue = new Dictionary<object, object>();

        switch (parser.Current)
        {
            case Scalar:
            {
                var value = parser.Consume<Scalar>().Value;
                defaultValue.Add("value", value);
                break;
            }
            case MappingStart:
            {
                parser.Consume<MappingStart>();
                while (!parser.TryConsume<MappingEnd>(out _))
                {
                    var key = parser.Consume<Scalar>().Value;
                    var value = parser.Consume<Scalar>().Value;
                    defaultValue.Add(key, value);
                }

                break;
            }
        }

        return defaultValue;
    }

    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        throw new NotImplementedException();
    }
}