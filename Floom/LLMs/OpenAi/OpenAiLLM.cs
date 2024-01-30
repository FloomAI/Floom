using System.Text.Json.Serialization;
using Floom.Model.OpenAi;
using Microsoft.AspNetCore.Mvc;

namespace Floom.LLMs.OpenAi
{
    public class OpenAiLLM : LLMProvider, LLMImageProvider
    {
        private readonly OpenAiClient _openAiClient;
        private readonly ILogger<OpenAiLLM> _logger;

        public OpenAiLLM(ILogger<OpenAiLLM> logger, OpenAiClient openAiClient)
        {
            _logger = logger;
            _openAiClient = openAiClient;
        }

        public void SetApiKey(string apiKey)
        {
            _openAiClient.ApiKey = apiKey;
        }

        public async Task<PromptResponse> GenerateTextAsync(PromptRequest prompt, string model)
        {
            return await _openAiClient.GenerateTextAsync(prompt, model);
        }

        public async Task<IActionResult> ValidateModelAsync(string model)
        {
            _logger.LogInformation("ValidateModelAsync");
            return await _openAiClient.ValidateModelAsync(model);
        }

        public async Task<PromptResponse> GenerateImageAsync(PromptRequest prompt, string model)
        {
            return await _openAiClient.GenerateImageAsync(prompt, model);
        }
    }

    public class GenerateImageRequestMessage
    {
        public string? prompt { get; set; }
        public string? size { get; set; }
        public uint? n { get; set; } //Options

        public string response_format { get; set; } = "b64_json";
    }

    public class GenerateTextRequestMessage
    {
        public string model { get; set; }
        public List<Message> messages { get; set; }
        public double temperature { get; set; }
    }

    public class Message
    {
        public string? role { get; set; }
        public string? content { get; set; }
    }

    public class GenerateTextResponseMessage
    {
        public string? id { get; set; }

        [JsonPropertyName("object")] public string? Object { get; set; }

        public long created { get; set; }
        public string? model { get; set; }
        public Usage? usage { get; set; }
        public List<Choice>? choices { get; set; }
    }


    public class GenerateImageResponseMessage
    {
        public long created { get; set; }
        public List<ImageData>? data { get; set; }
    }

    public class ImageData
    {
        public string? url { get; set; }
        public string? b64_json { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class Choice
    {
        public Message? message { get; set; }
        public string? finish_reason { get; set; }
        public int index { get; set; }
    }
}