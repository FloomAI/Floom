using System.Text.Json.Serialization;
using Floom.Logs;
using Floom.Model.OpenAi;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Embeddings.OpenAi;

public class OpenAiEmbeddings : EmbeddingsProvider
{
    private readonly OpenAiClient _openAiClient = new();
    private readonly ILogger _logger;
    public string Model { get; set; } = "text-embedding-ada-002";

    public OpenAiEmbeddings()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }
    
    public void SetApiKey(string apiKey)
    {
        _logger.LogInformation("SetApiKey");
        _openAiClient.ApiKey = apiKey;
    }

    public async Task<List<List<float>>> GetEmbeddingsAsync(List<string> strings)
    {
        _logger.LogInformation("GetEmbeddingsAsync");
        return await _openAiClient.GetEmbeddingsAsync(strings);
    }

    public async Task<IActionResult> ValidateModelAsync()
    {
        _logger.LogInformation("ValidateModelAsync");
        return await _openAiClient.ValidateModelAsync(Model);
    }

    public string GetModelName()
    {
        return Model;
    }

    public class EmbeddingResponse
    {
        [JsonPropertyName("object")] public string? Object { get; set; }

        public List<EmbeddingData>? data { get; set; }
        public string? model { get; set; }
        public Usage? usage { get; set; }
    }

    public class EmbeddingData
    {
        [JsonPropertyName("object")] public string? Object { get; set; }
        public List<float>? embedding { get; set; }
        public int index { get; set; }
    }

    public class EmbeddingRequest
    {
        public List<string>? input { get; set; }
        public string? model { get; set; }
    }
}