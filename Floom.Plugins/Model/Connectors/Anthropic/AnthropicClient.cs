using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

    public async Task<ModelConnectorResult> GenerateTextAsync(FloomRequest promptRequest, string model)
    {
        var requestUri = $"{MainUrl}/messages";
        var payload = new AnthropicRequest
        {
            model = model,
            max_tokens = 1024,
            messages = new List<AnthropicMessage>
            {
                new() { role = "user", content = promptRequest.Prompt.UserPrompt }
            }
        };

        if (!string.IsNullOrEmpty(promptRequest.Prompt.UserPromptAddon))
        {
            payload.messages.Add(new AnthropicMessage { role = "user", content = promptRequest.Prompt.UserPromptAddon });
        }
        
        if(!string.IsNullOrEmpty(promptRequest.Prompt.SystemPrompt))
        {
            payload.system = promptRequest.Prompt.SystemPrompt;
        }

        if (!string.IsNullOrEmpty(promptRequest.Context?.Context))
        {
            payload.system += " " + promptRequest.Context.Context;
        }
        
        using (var httpClient = new HttpClient())
        {
            var requestContent = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
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

            var responseValues = generatedTextParts?.Select(text => new ResponseValue
            {
                type = DataType.String,
                value = text
            }).ToList();

            if(generatedTextParts == null || !generatedTextParts.Any())
            {
                _logger.LogError("Anthropic API call returned no content");
                return new ModelConnectorResult { Success = false, Message = "Anthropic Error Generating Text" };
            }

            return new ModelConnectorResult
            {
                Success = true,
                Data = new ResponseValue()
                {
                    type = DataType.String,
                    value = generatedTextParts.FirstOrDefault()
                }
            };
        }
    }
}


public class AnthropicRequest
{
    public string model { get; set; }
    public int max_tokens { get; set; }
    public List<AnthropicMessage> messages { get; set; }
    public string system { get; set; }
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