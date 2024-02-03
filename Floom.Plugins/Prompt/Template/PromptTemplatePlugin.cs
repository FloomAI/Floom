using Floom.Model;
using Floom.Pipeline;
using Floom.Pipeline.Prompt;
using Floom.Plugin.Base;
using Floom.Plugin.Context;
using Floom.Utils;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Prompt.Template;

public class PromptTemplatePluginSettings : FloomPluginConfigBase
{
    public string? System { get; private set; }
    public string? User { get; private set; }

    public PromptTemplatePluginSettings(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        System = configuration.TryGetValue("system", out var apiKey) ? apiKey as string : string.Empty;
        User = configuration.TryGetValue("user", out var model) ? model as string : string.Empty;
    }
}

[FloomPlugin("floom/prompt/template/default")]
public class PromptTemplatePlugin: FloomPluginBase
{
    private PromptTemplatePluginSettings _settings;

    public PromptTemplatePlugin()
    {
    }
    
    public override void Initialize(PluginContext context)
    {
        _logger.LogInformation($"Initializing {GetType()}");

        // Initialize settings with specific plugin settings class
        _settings = new PromptTemplatePluginSettings(context.Configuration.Configuration);
    }

    public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing {GetType()}: {pluginContext.Package}");

        var pluginConfiguration = pluginContext.Configuration;

        var promptRequest = new FloomPromptRequest();
        
        if (_settings.System != null)
        {
            promptRequest.system = _settings.System.CompileWithVariables(pipelineContext.Request.variables);
        }

        if(_settings.User != null)
        {
            promptRequest.user = _settings.User.CompileWithVariables(pipelineContext.Request.variables);
        }

        if (pipelineContext.Request.input != null)
        {
            promptRequest.user = pipelineContext.Request.input.CompileWithVariables(pipelineContext.Request.variables);
        }
        
        _logger.LogInformation($"Completed {GetType()} Successfully");

        pipelineContext.AddEvent(new PromptTemplateResultEvent 
        { 
            Timestamp = DateTime.UtcNow,
            ResultData = promptRequest,
        });

        return new PluginResult
        {
            Success = true,
        };
    }

}