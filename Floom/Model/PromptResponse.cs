using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;

namespace Floom.Model;

public class PromptResponse
{
    public List<ResponseValue> values { get; set; } = new List<ResponseValue>();
    public long elapsedProcessingTime { get; set; }
    public PromptTokenUsage tokenUsage { get; set; }
    public PromptTokenUsage cost { get; set; }
}

public class PromptTokenUsage
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

    public class PromptCost
    {
        public string currency { get; set; }
        public decimal value { get; set; }
    }
}