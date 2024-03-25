using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Floom.Logs;
using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors.Anthropic;

public class AnthropicClient : IModelConnectorClient
{
    private readonly ILogger _logger;
    public string? ApiKey { get; set; }
    private const string API_VERSION = "2023-06-01";
    readonly string MainUrl = "https://api.anthropic.com/v1";

    public AnthropicClient()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    public async Task<ModelConnectorResult> GenerateTextAsync(FloomRequest floomRequest, string model)
    {
        var requestUri = $"{MainUrl}/messages";
        var payload = new AnthropicRequest
        {
            model = model,
            max_tokens = 1024,
            messages = new List<AnthropicMessage>
            {
                new() { role = "user", content = floomRequest.Prompt.UserPrompt }
            }
        };

        if (!string.IsNullOrEmpty(floomRequest.Prompt.UserPromptAddon))
        {
            payload.system = floomRequest.Prompt.UserPromptAddon;
        }
        
        if(!string.IsNullOrEmpty(floomRequest.Prompt.SystemPrompt))
        {
            if (!string.IsNullOrEmpty(payload.system))
            {
                payload.system += " " + floomRequest.Prompt.SystemPrompt;
            }
            else
            {
                payload.system = floomRequest.Prompt.SystemPrompt;
            }
        }

        if (!string.IsNullOrEmpty(floomRequest.Context?.Context))
        {
            if (!string.IsNullOrEmpty(payload.system))
            {
                payload.system += " " + floomRequest.Prompt.SystemPrompt;
            }
            else
            {
                payload.system = floomRequest.Prompt.SystemPrompt;
            }
        }
        
        using (var httpClient = new HttpClient())
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            };

            var jsonBody = JsonSerializer.Serialize(payload, options);

            var requestContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", API_VERSION);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await httpClient.PostAsync(requestUri, requestContent);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Anthropic API call failed with status code {response.StatusCode} and message {responseString}");
                return new ModelConnectorResult { Success = false, Message = "Anthropic Error Generating Text" };
            }

            var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var generatedTextParts = anthropicResponse?.Content?.Select(c => c.Text).ToList();
            
            if(generatedTextParts == null || !generatedTextParts.Any())
            {
                _logger.LogError("Anthropic API call returned no content");
                return new ModelConnectorResult { Success = false, Message = "Anthropic Error Generating Text" };
            }

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
                var tempString = generatedTextParts.FirstOrDefault();
                
                try
                {
                    var jsonElement = JsonSerializer.Deserialize<JsonElement>(tempString);
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
}


public class AnthropicRequest
{
    public string model { get; set; }
    public int max_tokens { get; set; }
    public List<AnthropicMessage> messages { get; set; }
    public string? system { get; set; }
}

public class AnthropicMessage
{
    public string role { get; set; }
    public string content { get; set; }
}

public class AnthropicResponse
{
    public List<Content> Content { get; set; }
    public string Id { get; set; }
    public string Model { get; set; }
    public string Role { get; set; }
    public string StopReason { get; set; }
    public string StopSequence { get; set; }
    public string Type { get; set; }
    public Usage Usage { get; set; }
}

public class Content
{
    public string Text { get; set; }
    public string Type { get; set; }
}

public class Usage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
}