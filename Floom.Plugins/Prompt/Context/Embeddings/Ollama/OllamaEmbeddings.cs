using Floom.Base;
using Floom.Logs;
using Floom.Plugins.Model.Connectors.Ollama;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Prompt.Context.Embeddings.Ollama;

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

    public async Task<FloomOperationResult<List<List<float>>>> GetEmbeddingsAsync(List<string> strings)
    {
        return await _ollamaClient.GetEmbeddingsAsync(strings);
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