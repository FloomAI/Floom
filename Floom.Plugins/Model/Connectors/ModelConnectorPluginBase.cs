using Floom.Model;
using Floom.Pipeline;
using Floom.Pipeline.Prompt;
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
    }

    protected abstract void InitializeClient(ModelConnectorPluginConfig settings);

    protected FloomPromptRequest? FinalizePromptRequest(PipelineContext pipelineContext)
    {
        var promptTemplateResultEvent = pipelineContext.GetEvents()
            .OfType<PromptTemplateResultEvent>()
            .FirstOrDefault()
            ?.ResultData;

        var promptContextResultEvent = pipelineContext.GetEvents()
            .OfType<PromptContextResultEvent>()
            .FirstOrDefault()
            ?.ResultData;

        return promptContextResultEvent ?? promptTemplateResultEvent;
    }
    
    protected virtual FloomPromptResponse ProcessPromptRequest(PipelineContext pipelineContext, FloomPromptRequest promptRequest)
    {
        return _client.GenerateTextAsync(promptRequest, _settings.Model).Result;
    }
    
    public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing {GetType()}: {pluginContext.Package}");

        // Determine the final promptRequest based on the available events
        var promptRequest = FinalizePromptRequest(pipelineContext);

        if (_settings.Model != null && promptRequest != null)
        {
            var response = ProcessPromptRequest(pipelineContext, promptRequest);

            _logger.LogInformation($"{GetType()} Completed Successfully");

            return new PluginResult()
            {
                Success = true,
                ResultData = response
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
    Task<FloomPromptResponse> GenerateTextAsync(FloomPromptRequest promptRequest, string model);
}