using System.Text;
using Microsoft.AspNetCore.Mvc.Formatters;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Floom.Utils;

public class YamlInputFormatter : TextInputFormatter
{
    public YamlInputFormatter()
    {
        SupportedMediaTypes.Add("text/yaml");
        SupportedEncodings.Add(Encoding.UTF8);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context,
        Encoding encoding)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
        var yaml = await reader.ReadToEndAsync();

        var deserializationOptions = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new PluginYamlTypeConverter())
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        try
        {
            var result = deserializationOptions.Deserialize(new StringReader(yaml), context.ModelType);
            return await InputFormatterResult.SuccessAsync(result);
        }
        catch (YamlException ex)
        {
            // Handle deserialization error
            return await InputFormatterResult.FailureAsync();
        }
    }

    protected override bool CanReadType(Type type) => true;
}