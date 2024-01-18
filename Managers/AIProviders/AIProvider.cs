using Floom.Models.Other.Floom;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Managers.AIProviders
{
    public abstract class AIProvider
    {
        //public abstract Task<List<List<float>>> GetEmbeddingsAsync(List<string> strings);

        //public abstract Task<PromptResponse> GenerateTextAsync(PromptRequest prompt, string model);

        //public abstract async Task<PromptResponse> GenerateImageAsync(PromptRequest prompt, string model);
    }


    [BsonIgnoreExtraElements]
    public class PromptRequest
    {
        public string? system { get; set; }
        public string? user { get; set; }
        public List<PromptMessage> previousMessages { get; set; } = new List<PromptMessage>();
        
        //Image
        public string resolution { get; set; }
        public uint options { get; set; } = 1;
    }

    public class PromptMessage
    {
        public string role { get; set; } = string.Empty;
        public string content { get; set; } = string.Empty;
    }

    [BsonIgnoreExtraElements]
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

}
