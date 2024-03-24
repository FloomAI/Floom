using Floom.Model;
using Floom.Pipeline;
using Floom.Pipeline.Stages.Prompt;
using Floom.Plugin.Base;
using Floom.Plugin.Context;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors;

public abstract class ModelConnectorPluginBase<TClient> : FloomPluginBase where TClient : IModelConnectorClient, new()
{
    protected readonly TClient _client = new();
    protected ModelConnectorPluginConfig _settings;

    public override void Initialize(PluginContext context)
    {
        _logger.LogInformation($"Initializing {GetType()}");

        // Initialize settings with specific plugin settings class
        _settings = new ModelConnectorPluginConfig(context.Configuration.Configuration);
        InitializeClient(_settings);

        if (string.IsNullOrEmpty(_settings.Voice) && context.Manifest != null)
        {
            if (context.Manifest.Parameters.TryGetValue("voice", out var voiceParameter))
            {
                if (voiceParameter.DefaultValue is IDictionary<object, object> voiceDefaultValue)
                {
                    _settings.Voice = voiceDefaultValue.TryGetValue("value", out var voice) ? voice as string : string.Empty;
                }
            }
        }
    }

    protected abstract void InitializeClient(ModelConnectorPluginConfig settings);

    protected FloomRequest? FinalizePromptRequest(PipelineContext pipelineContext)
    {
        var promptStageResult = pipelineContext.GetEvents()
            .OfType<PromptStageResultEvent>()
            .FirstOrDefault()
            ?.ResultData;
        
        return promptStageResult;
    }
    
    protected virtual ModelConnectorResult ProcessPromptRequest(PipelineContext pipelineContext, FloomRequest floomRequest)
    {
        return _client.GenerateTextAsync(floomRequest, _settings.Model).Result;
    }
    
    public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing {GetType()}: {pluginContext.Package}");

        // Determine the final promptRequest based on the available events
        var promptRequest = FinalizePromptRequest(pipelineContext);

        if (_settings.Model != null && promptRequest != null)
        {
            var response = ProcessPromptRequest(pipelineContext, promptRequest);
            
            if (response.Success)
            {
                _logger.LogInformation($"{GetType()} Completed Successfully");
                
                return new PluginResult()
                {
                    Success = true,
                    Data = response
                };
            }

            _logger.LogError($"{GetType()} Failed: {response.Message}");
                
            return new PluginResult()
            {
                Success = false,
                Data = response,
                Message = response.Message
            };
        }
        
        if(_settings.Model == null)
            _logger.LogError($"{GetType()} Model is not set");
        
        if(promptRequest == null)
            _logger.LogError($"{GetType()} PromptRequest is not set");
        
        return new PluginResult()
        {
            Success = false
        };
    }
}

public interface IModelConnectorClient
{
    Task<ModelConnectorResult> GenerateTextAsync(FloomRequest floomRequest, string model);
}
