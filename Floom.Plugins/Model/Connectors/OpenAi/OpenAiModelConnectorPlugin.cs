using Floom.Model;
using Floom.Pipeline;
using Floom.Plugin.Base;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors.OpenAi;

[FloomPlugin("floom/model/connector/openai")]
public class OpenAiModelConnectorPlugin : ModelConnectorPluginBase<OpenAiClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        _client.ApiKey = settings.ApiKey;
    }
    
    protected override FloomPromptResponse ProcessPromptRequest(PipelineContext pipelineContext, FloomPromptRequest promptRequest)
    {
        var responseConfig = pipelineContext.Pipeline.Response?.Format?.First();

        var responseType = "text";
        
        if (responseConfig != null)
        {
            var responseTypeConfig = responseConfig.Configuration.GetValueOrDefault("type", "text");
            responseType = responseTypeConfig.ToString();
        }
        
        if (responseType == "text")
        {
            return _client.GenerateTextAsync(promptRequest, _settings.Model).Result;
        }

        if(responseType == "image")
        {
            return _client.GenerateImageAsync(promptRequest, _settings.Model).Result;
        }
        
        _logger.LogError($"Invalid response type: {responseType}");
        
        return new FloomPromptResponse();
    }
}