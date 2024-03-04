using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Floom.Logs;
using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors.Anthropic;

public class AnthropicClient : IModelConnectorClient
{
    private readonly ILogger _logger;
    public string? ApiKey { get; set; }
    private const string API_VERSION = "2023-06-01";

    public AnthropicClient()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }

    public async Task<FloomPromptResponse> GenerateTextAsync(FloomPromptRequest promptRequest, string model)
    {
        var requestUri = "https://api.anthropic.com/v1/messages";
        var payload = new
        {
            model = model,
            max_tokens = 1024,
            messages = new List<object>
            {
                new { role = "user", content = promptRequest.user }
            }
        };

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
                return new FloomPromptResponse { success = false, message = "Anthropic Error Generating Text" };
            }

            var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseString, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var generatedTextParts = anthropicResponse?.Content?.Select(c => c.Text).ToList();

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