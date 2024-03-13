using Floom.Base;
using Floom.Logs;
using Floom.Plugins.Model.Connectors.Gemini;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Prompt.Context.Embeddings.Gemini;

public class GeminiEmbeddings : EmbeddingsProvider
{
    private readonly GeminiClient _geminiClient = new();
    private readonly ILogger _logger;
    public string Model { get; set; } = "embedding-001";

    public GeminiEmbeddings()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }
    
    public void SetApiKey(string apiKey)
    {
        _logger.LogInformation("SetApiKey");
        _geminiClient.ApiKey = apiKey;
    }
    
    public async Task<FloomOperationResult<List<List<float>>>> GetEmbeddingsAsync(List<string> strings)
    {
        _logger.LogInformation("GetEmbeddingsAsync called with model {Model}", Model);
        try
        {
            return await _geminiClient.GetEmbeddingsAsync(strings, Model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get embeddings");
            return FloomOperationResult<List<List<float>>>.CreateFailure(ex.Message);
        }
    }

    public async Task<IActionResult> ValidateModelAsync()
    {
        return await _geminiClient.ValidateModelAsync(Model);
    }

    public string GetModelName()
    {
        return Model;
    }
}