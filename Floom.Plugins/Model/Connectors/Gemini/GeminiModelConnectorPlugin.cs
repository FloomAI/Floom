using Floom.Model;
using Floom.Pipeline;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;
using Floom.Plugin.Base;

namespace Floom.Plugins.Model.Connectors.Gemini;

[FloomPlugin("floom/model/connector/gemini")]
public class GeminiModelConnectorPlugin : ModelConnectorPluginBase<GeminiClient>
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
            return _client.GenerateTextAsync(floomRequest, _settings.Model).Result;
        }
        
        return new ModelConnectorResult
        {
            Success = false,
            Message = $"Error, Gemini Does not support response type: {responseType}",
            ErrorCode = ModelConnectorErrors.UnsupportedResponseType
        };
    }
}