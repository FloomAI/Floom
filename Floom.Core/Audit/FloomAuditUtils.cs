using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;

namespace Floom.Audit;

public class FloomAuditUtils
{
    public static Dictionary<string, object> GetPipelineAttributes(
        RunFloomPipelineRequest runFloomPipelineRequest,
        FloomRequest promptRequest,
        FloomPromptResponse promptResponse)
    {
        var attributes = new Dictionary<string, object>();

        if (runFloomPipelineRequest.prompt != null)
            attributes.Add("request", runFloomPipelineRequest.prompt);
        if (runFloomPipelineRequest.variables != null)
            attributes.Add("requestVariables", runFloomPipelineRequest.variables);
        if (promptRequest.Prompt?.UserPrompt != null)
            attributes.Add("compiledPromptRequestUser", promptRequest.Prompt.UserPrompt);
        if (promptRequest.Prompt?.SystemPrompt != null)
            attributes.Add("compiledPromptRequestSystem", promptRequest.Prompt.SystemPrompt);
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