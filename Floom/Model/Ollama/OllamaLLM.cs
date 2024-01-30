using Floom.Model;
using Floom.Vendors;
using Microsoft.AspNetCore.Mvc;

namespace Floom.LLMs.Ollama;

public class OllamaLLM
{
    private readonly OllamaClient _ollamaClient;
    private readonly ILogger<OllamaLLM> _logger;

    public OllamaLLM(ILogger<OllamaLLM> logger, OllamaClient ollamaClient)
    {
        _logger = logger;
        _ollamaClient = ollamaClient;
    }

    public void SetApiKey(string apiKey)
    {
    }

    public Task<PromptResponse> GenerateTextAsync(PromptRequest prompt, string model)
    {
        return _ollamaClient.GenerateTextAsync(prompt, model);
    }

    public Task<IActionResult> ValidateModelAsync(string model)
    {
        return Task.FromResult<IActionResult>(new OkObjectResult(new { Message = $"Model valid" }));
    }
}