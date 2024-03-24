using Floom.Pipeline;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Model;
using Floom.Plugin.Base;
using Floom.Plugin.Context;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Response.Formatter;

public class ResponseFormatterPluginSettings : FloomPluginConfigBase
{
    public string? Type { get; private set; }

    public ResponseFormatterPluginSettings(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        Type = configuration.TryGetValue("type", out var type) ? type as string : string.Empty;
    }
}

[FloomPlugin("floom/response/formatter")]
public class ResponseFormatterPlugin: FloomPluginBase
{
    ResponseFormatterPluginSettings _settings;
    
    public override void Initialize(PluginContext context)
    {
        _logger.LogInformation($"Initializing {GetType()}");

        // Initialize settings with specific plugin settings class
        _settings = new ResponseFormatterPluginSettings(context.Configuration.Configuration);
    }
    
    public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing Plugin {pluginContext.Package}");

        // Logic to handle response formatter

        var modelStageResult = pipelineContext.GetEvents().OfType<ModelStageResultEvent>();
            
        var modelConnectorResult = modelStageResult.FirstOrDefault()?.Response;
            
        var responseFormatterResult = new ResponseFormatterResult
        {
            value = modelConnectorResult?.Data
        };
        
        return new PluginResult
        {
            Success = true,
            Data = responseFormatterResult
        };
    }
}