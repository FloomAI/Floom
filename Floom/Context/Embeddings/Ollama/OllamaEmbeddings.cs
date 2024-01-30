using Floom.Logs;
using Floom.Model.Ollama;
using Floom.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Embeddings.Ollama;

public class OllamaEmbeddings : EmbeddingsProvider
{
    private readonly OllamaClient _ollamaClient = new();
    private readonly ILogger _logger;
    public string Model { get; set; } = "";

    public OllamaEmbeddings()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }
    
    public class EmbeddingRequest
    {
        public string? model { get; set; }
        public string? prompt { get; set; }
    }

    public class EmbeddingResponse
    {
        public List<float>? embedding { get; set; }
    }

    public Task<List<List<float>>> GetEmbeddingsAsync(List<string> strings)
    {
        return _ollamaClient.GetEmbeddingsAsync(strings);
    }

    public Task<IActionResult> ValidateModelAsync()
    {
        return _ollamaClient.ValidateModelAsync(GetModelName());
    }

    public string GetModelName()
    {
        throw new NotImplementedException();
    }
}