using System.Text;
using System.Text.Json;
using Floom.Base;
using Floom.Logs;
using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors.Gemini;

public class GeminiClient : IModelConnectorClient
{
    private readonly ILogger _logger;
    public string? ApiKey { get; set; }

    public GeminiClient()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    public async Task<FloomPromptResponse> GenerateTextAsync(FloomPromptRequest promptRequest, string model)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={ApiKey}";
        var payload = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = promptRequest.user }
                    }
                }
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error generating text: {responseContent}");
                return new FloomPromptResponse { success = false, message = "Error generating text" };
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // This makes the deserialization process case-insensitive
            };

            var result = JsonSerializer.Deserialize<GeminiGenerateTextResponse>(responseContent , options);

            var generatedTextParts = result.Candidates.FirstOrDefault()?.Content.Parts.Select(p => p.Text).ToList();

            var responseValues = generatedTextParts?.Select(text => new ResponseValue
            {
                type = DataType.String,
                value = text
            }).ToList();

            return new FloomPromptResponse
            {
                success = true,
                values = responseValues ?? new List<ResponseValue>()
            };
        }
    }


    public async Task<FloomOperationResult<List<List<float>>>> GetEmbeddingsAsync(List<string> strings)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/embedding-001:embedContent?key={ApiKey}";
        var payload = new
        {
            model = "models/embedding-001",
            content = new
            {
                parts = strings.Select(s => new { text = s }).ToArray()
            }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error generating embeddings: {responseContent}");
                return FloomOperationResult<List<List<float>>>.CreateFailure("Error generating embeddings");
            }

            var result = JsonSerializer.Deserialize<GeminiGenerateEmbeddingsResponse>(responseContent);

            var embeddings = result.Embeddings.Select(e => e.Vector).ToList();
            return FloomOperationResult<List<List<float>>>.CreateSuccessResult(embeddings);
        }
    }
}

// Example response classes, adjust these according to the actual JSON structure of the API responses
public class GeminiGenerateTextResponse
{
    public List<ResponseCandidate> Candidates { get; set; }
    public PromptFeedback PromptFeedback { get; set; }
}

public class ResponseCandidate
{
    public ResponseContent Content { get; set; }
    public string FinishReason { get; set; }
    public int Index { get; set; }
    public List<SafetyRating> SafetyRatings { get; set; }
}

public class ResponseContent
{
    public List<ResponsePart> Parts { get; set; }
    public string Role { get; set; }
}

public class ResponsePart
{
    public string Text { get; set; }
}

public class SafetyRating
{
    public string Category { get; set; }
    public string Probability { get; set; }
}

public class PromptFeedback
{
    public List<SafetyRating> SafetyRatings { get; set; }
}

public class GeminiGenerateEmbeddingsResponse
{
    public List<Embedding> Embeddings { get; set; }

    public class Embedding
    {
        public List<float> Vector { get; set; }
    }
}
