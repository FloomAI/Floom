using Floom.Pipeline.Entities.Dtos;

namespace Floom.Model;

public class FloomPromptResponse
{
    public bool success { get; set; }
    public string? message { get; set; }
    public int? errorCode { get; set; }
    public List<ResponseValue> values { get; set; } = new();
    public long elapsedProcessingTime { get; set; }
    public FloomPromptTokenUsage tokenUsage { get; set; }
    public FloomPromptTokenUsage cost { get; set; }
}

public class FloomPromptTokenUsage
{
    public int processingTokens { get; set; }
    public int promptTokens { get; set; }
    public int totalTokens { get; set; }

    public FloomResponseTokenUsage ToFloomResponseTokenUsage()
    {
        return new FloomResponseTokenUsage
        {
            promptTokens = promptTokens,
            totalTokens = totalTokens,
            processingTokens = processingTokens,
        };
    }
}