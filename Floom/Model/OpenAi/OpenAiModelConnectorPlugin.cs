using Floom.LLMs;
using Floom.Pipeline;
using Floom.Pipeline.Model;
using Floom.Pipeline.Prompt;
using Floom.Plugin;

namespace Floom.Model.OpenAi;

public class OpenAiModelConnectorPluginSettings : FloomPluginConfigBase
{
    public string? ApiKey { get; private set; }
    public string? Model { get; private set; }

    public OpenAiModelConnectorPluginSettings(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        ApiKey = configuration.TryGetValue("apikey", out var apiKey) ? apiKey as string : string.Empty;
        Model = configuration.TryGetValue("model", out var model) ? model as string : string.Empty;
    }
}

[FloomPlugin("floom/model/connector/openai")]
public class OpenAiModelConnectorPlugin : ModelConnectorPluginBase
{
    private readonly OpenAiClient _openAiClient = new();
    private OpenAiModelConnectorPluginSettings _settings;
    
    public override void Initialize(PluginContext context)
    {
        _logger.LogInformation($"Initializing {GetType()}");

        // Initialize settings with specific plugin settings class
        _settings = new OpenAiModelConnectorPluginSettings(context.Configuration.Configuration);
        _openAiClient.ApiKey = _settings.ApiKey;
    }
    
    public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing {GetType()}: {pluginContext.Package}");
        
        var promptTemplateResultEvent = pipelineContext.GetEvents()
            .OfType<PromptTemplateResultEvent>()
            .FirstOrDefault()
            ?.ResultData;

        var promptContextResultEvent = pipelineContext.GetEvents()
            .OfType<PromptContextResultEvent>()
            .FirstOrDefault()
            ?.ResultData;

        // Determine the final promptRequest based on the available events
        var promptRequest = promptContextResultEvent ?? promptTemplateResultEvent;
        
        if (_settings.Model != null)
        {
            var response = await _openAiClient.GenerateTextAsync(promptRequest, _settings.Model);
            
            _logger.LogInformation($"{GetType()} Completed Successfully");
            
            return new PluginResult()
            {
                Success = true,
                ResultData = response
            };
        }

        _logger.LogInformation($"{GetType()} Completed With Errors");

        return new PluginResult()
        {
            Success = false
        };
    }
}