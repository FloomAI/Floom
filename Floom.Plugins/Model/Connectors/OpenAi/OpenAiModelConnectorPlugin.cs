using Floom.Model;
using Floom.Pipeline;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;
using Floom.Plugin.Base;

namespace Floom.Plugins.Model.Connectors.OpenAi;

[FloomPlugin("floom/model/connector/openai")]
public class OpenAiModelConnectorPlugin : ModelConnectorPluginBase<OpenAiClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        _client.ApiKey = settings.ApiKey;
    }
    
    protected override ModelConnectorResult ProcessPromptRequest(PipelineContext pipelineContext, FloomRequest floomRequest)
    {
        var responseType = floomRequest.Prompt.ResponseType;
        
        if (responseType is DataType.String or DataType.JsonObject)
        {
            if(_settings.Model.Contains("whisper"))
            {
                return _client.GenerateSpeechToTextAsync(floomRequest.Prompt.File, _settings.Model).Result;
            }

            return _client.GenerateTextAsync(floomRequest, _settings.Model).Result;
        }

        if(responseType is DataType.Image)
        {
            return _client.GenerateImageAsync(floomRequest, _settings.Model).Result;
        }
        
        if(responseType is DataType.Audio)
        {
            return _client.GenerateTextToSpeechAsync(floomRequest, _settings.Model, _settings.Voice).Result;
        }
        
        return new ModelConnectorResult
        {
            Success = false,
            Message = $"Error, OpenAI Does not support response type: {responseType}",
            ErrorCode = ModelConnectorErrors.UnsupportedResponseType
        };
    }
}