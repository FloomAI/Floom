using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Floom.Base;
using Floom.Logs;
using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors.Gemini;

public class GeminiClient : IModelConnectorClient
{
    readonly string MainUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    private readonly ILogger _logger;
    public string? ApiKey { get; set; }

    public GeminiClient()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    public async Task<ModelConnectorResult> GenerateTextAsync(FloomRequest floomRequest, string model)
    {
        var url = $"{MainUrl}/{model}:generateContent?key={ApiKey}";
        
        // Initialize the parts list with the user text by default
        var partsList = new List<GeminiPart> { new GeminiPart { text = floomRequest.Prompt.UserPrompt } };

        if (!string.IsNullOrEmpty(floomRequest.Prompt.UserPromptAddon))
        {
            partsList.Insert(0, new GeminiPart { text = floomRequest.Prompt.UserPromptAddon });
        }

        // If the system property exists, insert it as the first element of the parts list
        if (!string.IsNullOrEmpty(floomRequest.Prompt.SystemPrompt))
        {
            partsList.Insert(0, new GeminiPart { text = floomRequest.Prompt.SystemPrompt });
        }

        if (!string.IsNullOrEmpty(floomRequest.Context?.Context))
        {
            partsList.Insert(0, new GeminiPart { text = floomRequest.Context.Context });
        }

        var payload = new GeminiTextRequest()
        {
            contents = new GeminiContent
            {
                parts = partsList
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
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    return new ModelConnectorResult { Success = false, Message = "Gemini model is overloaded. Please try again later", ErrorCode = ModelConnectorErrors.ModelOverloaded };
                }
                return new ModelConnectorResult { Success = false, Message = "Error generating text" };
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // This makes the deserialization process case-insensitive
            };

            var result = JsonSerializer.Deserialize<GeminiGenerateTextResponse>(responseContent , options);

            var generatedTextParts = result.Candidates.FirstOrDefault()?.Content.Parts.Select(p => p.Text).ToList();
            
            var modelConnectorResult = new ModelConnectorResult();
            modelConnectorResult.Success = true;
            if (floomRequest.Prompt.ResponseType == DataType.String)
            {
                modelConnectorResult.Data = new ResponseValue()
                {
                    type = DataType.String,
                    format = ResponseFormat.FromDataType(DataType.String),
                    value = generatedTextParts.FirstOrDefault()
                };
            }
            else if(floomRequest.Prompt.ResponseType == DataType.JsonObject)
            {
                var responseString = generatedTextParts.FirstOrDefault();
                
                try
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(responseString);
                    modelConnectorResult.Data = new ResponseValue()
                    {
                        type = DataType.JsonObject,
                        format = ResponseFormat.FromDataType(DataType.JsonObject),
                        value = jsonElement
                    };
                }
                catch (JsonException)
                {
                    _logger.LogError("Error while deserializing response to JSON object");
                    return new ModelConnectorResult()
                    {
                        Success = false,
                        Message = "Error while deserializing response to JSON object",
                        ErrorCode = ModelConnectorErrors.InvalidResponseFormat
                    };
                }
            }

            return modelConnectorResult;
        }
    }

    public async Task<FloomOperationResult<List<List<float>>>> GetEmbeddingsAsync(List<string> strings, string model)
    {
        var url = $"{MainUrl}/{model}:batchEmbedContents?key={ApiKey}";
        var allEmbeddings = new List<List<float>>();

        // Split strings into batches of 100
        var googleApiMaxBatchSize = 100;
        for (var i = 0; i < strings.Count; i += googleApiMaxBatchSize)
        {
            var batch = strings.Skip(i).Take(googleApiMaxBatchSize).ToList();
            var payload = new GeminiEmbeddingPayload
            {
                requests = batch.Select(s => new GeminiEmbeddingRequest
                {
                    model = "models/" + model,
                    content = new GeminiContent
                    {
                        parts = new List<GeminiPart> { new() { text = s } }
                    }
                }).ToList()
            };

            var serializedPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            var content = new StringContent(serializedPayload, Encoding.UTF8, "application/json");

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Error generating embeddings in batch starting with index {i}: {responseContent}");
                    continue;
                }

                var result = JsonSerializer.Deserialize<GeminiGenerateEmbeddingsResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (result != null && result.Embeddings != null)
                {
                    allEmbeddings.AddRange(result.Embeddings.Select(e => e.Values).ToList());
                }
            }
        }

        if (allEmbeddings.Count == 0)
        {
            return FloomOperationResult<List<List<float>>>.CreateFailure("Error generating all embeddings");
        }

        return FloomOperationResult<List<List<float>>>.CreateSuccessResult(allEmbeddings);
    }

    public async Task<IActionResult> ValidateModelAsync(string model)
    {
        _logger.LogInformation("ValidateModelAsync");
        var url = $"{MainUrl}?key={ApiKey}";
        
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Call the API and get the response
            HttpResponseMessage response = await client.GetAsync(url);

            var responseCode = response.StatusCode;

            if (responseCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unable to authenticate to Gemini");
                return new BadRequestObjectResult(new
                    { Message = $"OpenAI: Unable to authenticate", ErrorCode = ModelConnectorErrors.InvalidApiKey });
            }

            if (responseCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("Invalid model name " + model);
                return new BadRequestObjectResult(new
                    { Message = $"Gemini: Invalid model name", ErrorCode = ModelConnectorErrors.InvalidApiKey });
            }
            
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var modelsResponse = JsonSerializer.Deserialize<ModelsResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var modelExists = modelsResponse?.Models?.Any(m => m.Name.Equals($"models/{model}", StringComparison.OrdinalIgnoreCase)) ?? false;

                if (!modelExists)
                {
                    _logger.LogError("Invalid model name " + model);
                    return new BadRequestObjectResult(new
                        { Message = $"Invalid model name: {model}", ErrorCode = ModelConnectorErrors.InvalidModelName });
                }

                _logger.LogInformation("Model valid");
                return new OkObjectResult(new { Message = "Model valid" });
            }

            // Handle other status codes appropriately
            return new BadRequestObjectResult(new
                { Message = "Failed to validate model due to an unexpected error", ErrorCode = ModelConnectorErrors.UnexpectedError });
        }
    }
}

public class GeminiTextRequest
{
    public GeminiContent contents { get; set; }
}

public class ModelsResponse
{
    public List<ModelInfo> Models { get; set; }
}

public class ModelInfo
{
    public string Name { get; set; }
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

public class GeminiEmbeddingPayload
{
    public List<GeminiEmbeddingRequest> requests { get; set; } = new();
}

public class GeminiEmbeddingRequest
{
    public string? model { get; set; }
    public GeminiContent content { get; set; }
}

public class GeminiContent
{
    public List<GeminiPart>? parts { get; set; }
}

public class GeminiPart
{
    public string? text { get; set; }
}

public class GeminiGenerateEmbeddingsResponse
{
    public List<GeminiEmbedding> Embeddings { get; set; }

    public class GeminiEmbedding
    {
        public List<float> Values { get; set; }
    }
}
