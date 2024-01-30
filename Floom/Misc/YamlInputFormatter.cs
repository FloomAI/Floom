using Microsoft.AspNetCore.Mvc.Formatters;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

public class YamlInputFormatter : TextInputFormatter
{
    private readonly IDeserializer _yamlDeserializer;

    public YamlInputFormatter(IDeserializer yamlDeserializer)
    {
        SupportedMediaTypes.Add("text/yaml");
        SupportedEncodings.Add(Encoding.UTF8);
        _yamlDeserializer = yamlDeserializer;
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
        var yaml = await reader.ReadToEndAsync();

        var deserializationOptions = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .Build();

        try
        {
            var result = _yamlDeserializer.Deserialize(new StringReader(yaml), context.ModelType);
            return await InputFormatterResult.SuccessAsync(result);
        }
        catch (YamlException ex)
        {
            // Handle deserialization error
            return await InputFormatterResult.FailureAsync();
        }
    }

    protected override bool CanReadType(System.Type type) => true;
}
