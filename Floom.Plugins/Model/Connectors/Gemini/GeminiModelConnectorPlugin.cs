using Floom.Model;
using Floom.Pipeline;
using Floom.Plugin.Base;

namespace Floom.Plugins.Model.Connectors.Gemini;

[FloomPlugin("floom/model/connector/gemini")]
public class GeminiModelConnectorPlugin : ModelConnectorPluginBase<GeminiClient>
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
        
        return new FloomPromptResponse()
        {
            success = false,
            message = $"Error, Gemini Does not support response type: {responseType}"
        };
    }
}