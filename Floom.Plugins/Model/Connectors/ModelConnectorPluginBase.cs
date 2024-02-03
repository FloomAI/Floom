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
            var response = await _client.GenerateTextAsync(promptRequest, _settings.Model);

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

public interface IModelConnectorClient
{
    Task<FloomPromptResponse> GenerateTextAsync(FloomPromptRequest promptRequest, string model);
}
