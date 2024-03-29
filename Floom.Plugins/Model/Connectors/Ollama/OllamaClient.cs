using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Floom.Base;
using Floom.Logs;
using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;
using Floom.Plugins.Prompt.Context.Embeddings.Ollama;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors.Ollama;

public class OllamaGenerateTextRequestMessage
{
    public string model { get; set; }
    public string prompt { get; set; }
    public Dictionary<string, object> options { get; set; }
    public bool stream { get; set; } = false;
}

public class OllamaGenerateTextResponseMessage
{
    public string? id { get; set; }
    public string? response { get; set; }
}

public class OllamaClient : IModelConnectorClient
{
    private readonly ILogger _logger;
    private const string MainUrl = "http://127.0.0.1:11434/api/";

    public OllamaClient()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }
    
    public async Task<ModelConnectorResult> GenerateTextAsync(FloomRequest prompt, string model)
    {
        var result = new ModelConnectorResult();
        
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create the request message
            var generateTextRequestMessage = new OllamaGenerateTextRequestMessage
            {
                model = model,
                options = new Dictionary<string, object>
                {
                    { "temperature", 0.9 },
                    // { "num_gpu", 0 },
                    { "top_k", 100 },
                    { "top_p", 0.95 }
                }
            };
            
            if (prompt.Prompt.SystemPrompt != null)
            {
                generateTextRequestMessage.prompt = prompt.Prompt.SystemPrompt;
            }
            
            if (prompt.Context.Context != null)
            {
                generateTextRequestMessage.prompt += prompt.Context.Context + "\n";
            }
            
            //Add User
            if (prompt.Prompt.UserPrompt != null)
            {
                generateTextRequestMessage.prompt += "\n" + prompt.Prompt.UserPrompt;
            }

            var requestBody = JsonSerializer.Serialize(generateTextRequestMessage);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Call the API and get the response
            var response = await client.PostAsync($"{MainUrl}generate", content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var chatResponse =
                JsonSerializer.Deserialize<OllamaGenerateTextResponseMessage>(responseContent);

            if (chatResponse == null)
            {
                throw new Exception("Error while receiving response from OpenAI");
            }

            //Fill Response
            result.Success = true;
            result.Data = new ResponseValue()
            {
                type = DataType.String,
                value = chatResponse.response
            };
        }
        
        return result;
    }

    public async Task<IActionResult> ValidateModelAsync(string model)
    {
        _logger.LogInformation("ValidateModelAsync");

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Call the API and get the response
            var response = await client.GetAsync($"{MainUrl}tags");

            var responseString = await response.Content.ReadAsStringAsync();

            var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(responseString);

            var modelsArray = responseJson?["models"] as JsonArray;

            foreach (var jsonNode in modelsArray)
            {
                if (jsonNode is JsonObject jsonObject)
                {
                    var modelName = jsonObject["name"]?.ToString();
                    if (modelName != model) continue;
                    _logger.LogInformation("Ollama model valid");
                    return new OkObjectResult(new { Message = $"Model valid" });
                }
            }
        }

        return new BadRequestObjectResult(new { Message = $"Model {model} Not Valid" });
    }

    public async Task<FloomOperationResult<List<List<float>>>> GetEmbeddingsAsync(List<string> strings)
    {
        var allEmbeddings = new List<List<float>>();

        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        foreach (var text in strings)
        {
            // Create the request message
            var request = new OllamaEmbeddings.EmbeddingRequest
            {
                prompt = text,
                model = "text-embedding-ada-002"
            };

            var requestBody = JsonSerializer.Serialize(request);
            var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Call the API and get the response
            var response = await client.PostAsync($"{MainUrl}embeddings", content);
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                var errorMessage = $"Error while receiving response from Ollama. Status Code: {response.StatusCode}";
                _logger.LogError(errorMessage);
                return FloomOperationResult<List<List<float>>>.CreateFailure(errorMessage);
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            var embeddingResponse =
                JsonSerializer.Deserialize<OllamaEmbeddings.EmbeddingResponse>(responseContent);

            if (embeddingResponse == null)
            {
                throw new Exception("Error while receiving response from Ollama");
            }

            // Handle the embedding response
            allEmbeddings.Add(embeddingResponse.embedding);
        }

        return FloomOperationResult<List<List<float>>>.CreateSuccessResult(allEmbeddings);
    }
}