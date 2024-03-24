using Floom.Pipeline;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;
using Floom.Plugin.Base;
using Floom.Plugin.Context;
using Floom.Utils;
using Microsoft.Extensions.Logging;
using DataType = Floom.Pipeline.Entities.Dtos.DataType;

namespace Floom.Plugins.Prompt.Template;

public class PromptTemplatePluginSettings : FloomPluginConfigBase
{
    public string? System { get; private set; }
    public string? User { get; private set; }

    public PromptTemplatePluginSettings(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        System = configuration.TryGetValue("system", out var system) ? system as string : string.Empty;
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

        var promptTemplateResult = new PromptTemplateResult();
        
        if (_settings.System != null)
        {
            promptTemplateResult.SystemPrompt = _settings.System.CompileWithVariables(pipelineContext.pipelineRequest.variables);
        }

        if(_settings.User != null)
        {
            promptTemplateResult.UserPrompt = _settings.User.CompileWithVariables(pipelineContext.pipelineRequest.variables);
        }

        if (pipelineContext.pipelineRequest.prompt != null)
        {
            promptTemplateResult.UserPrompt = pipelineContext.pipelineRequest.prompt.CompileWithVariables(pipelineContext.pipelineRequest.variables);
        }

        var responseFormatter = pipelineContext.Pipeline.Response?.Format?.First();

        if (responseFormatter != null)
        {
            responseFormatter.Configuration.TryGetValue("type", out var responseType);
            promptTemplateResult.ResponseType = ResponseFormat.FromString(responseType as string);

            if (promptTemplateResult.ResponseType == DataType.JsonObject)
            {
                promptTemplateResult.UserPromptAddon = "return a JSON array, with JSON objects (without anything else) that looks exactly like class attached.";
            }
            
            responseFormatter.Configuration.TryGetValue("format", out var responseFormat);
            promptTemplateResult.ResponseFormat = responseFormat as string;
            
            responseFormatter.Configuration.TryGetValue("maxCharacters", out var maxCharacters);
            promptTemplateResult.MaxCharacters = Convert.ToUInt32(maxCharacters);
            
            responseFormatter.Configuration.TryGetValue("maxSentences", out var maxSentences);
            promptTemplateResult.MaxSentences = Convert.ToUInt32(maxSentences);
            
            responseFormatter.Configuration.TryGetValue("language", out var language);
            promptTemplateResult.Language = language as string;
        }
        
        _logger.LogInformation($"Completed {GetType()} Successfully");
        
        return new PluginResult
        {
            Success = true,
            Data = promptTemplateResult
        };
    }

}