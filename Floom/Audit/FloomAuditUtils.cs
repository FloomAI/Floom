using Floom.Model;
using Floom.Pipeline.Entities.Dtos;

namespace Floom.Audit;

public class FloomAuditUtils
{
    public static Dictionary<string, object> GetPipelineAttributes(
        FloomRequest floomRequest,
        FloomPromptRequest promptRequest,
        FloomPromptResponse promptResponse)
    {
        var attributes = new Dictionary<string, object>();

        if (floomRequest.input != null)
            attributes.Add("request", floomRequest.input);
        if (floomRequest.variables != null)
            attributes.Add("requestVariables", floomRequest.variables);
        if (promptRequest.user != null)
            attributes.Add("compiledPromptRequestUser", promptRequest.user);
        if (promptRequest.system != null)
            attributes.Add("compiledPromptRequestSystem", promptRequest.system);
        if (promptResponse.values.FirstOrDefault()?.value != null)
            attributes.Add("promptResponse", promptResponse.values.First().value.ToString());
        if (promptResponse.tokenUsage?.processingTokens != null)
            attributes.Add("promptResponseProcessingTokens", promptResponse.tokenUsage.processingTokens);
        if (promptResponse.tokenUsage?.promptTokens != null)
            attributes.Add("promptResponsePromptTokens", promptResponse.tokenUsage.promptTokens);
        if (promptResponse.tokenUsage?.totalTokens != null)
            attributes.Add("promptResponseTotalTokens", promptResponse.tokenUsage.totalTokens);
        attributes.Add("promptResponseProcessingTime", promptResponse.elapsedProcessingTime);

        return attributes;
    }
}