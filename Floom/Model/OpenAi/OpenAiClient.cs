using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Floom.Embeddings.OpenAi;
using Floom.Logs;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Model.OpenAi;

public class OpenAiClient
{
    private ILogger _logger;
    readonly string MainUrl = "https://api.openai.com/v1/";
    public string? ApiKey { get; set; }

    public OpenAiClient()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }
    
    public async Task<PromptResponse> GenerateTextAsync(PromptRequest prompt, string model)
    {
        PromptResponse promptResponse = new PromptResponse();

        Stopwatch swPrompt = new Stopwatch();
        swPrompt.Start();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create the request message
            GenerateTextRequestMessage generateTextRequestMessage = new GenerateTextRequestMessage
            {
                model = model,
                messages = new List<Message>(),
                temperature = 0.5
            };

            //Add messages

            //Add History (System+User+Assistant)

            //Add all messages
            foreach (PromptMessage promptMessage in prompt.previousMessages)
            {
                generateTextRequestMessage.messages.Add(new Message()
                {
                    role = promptMessage.role,
                    content = promptMessage.content
                });
            }

            //If history empty OR first in history not system
            if (prompt.previousMessages.Count == 0 || prompt.previousMessages.First().role != "system")
            {
                //take system from prompt
                if (prompt.system != null)
                {
                    generateTextRequestMessage.messages.Add(new Message()
                    {
                        role = "system",
                        content = prompt.system
                    });
                }
            }

            //dd
            //Add User
            if (prompt.user != null)
            {
                generateTextRequestMessage.messages.Add(new Message()
                {
                    role = "user",
                    content = prompt.user
                });
            }

            string requestBody = JsonSerializer.Serialize(generateTextRequestMessage);
            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Call the API and get the response
            _logger.LogInformation("Calling OpenAI API {0}", $"{MainUrl}chat/completions");
            HttpResponseMessage response = await client.PostAsync($"{MainUrl}chat/completions", content);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            GenerateTextResponseMessage? chatResponse =
                JsonSerializer.Deserialize<GenerateTextResponseMessage>(responseContent);

            if (chatResponse == null)
            {
                throw new Exception("Error while receiving response from OpenAI");
            }

            //Fill Response
            promptResponse = new PromptResponse()
            {
                elapsedProcessingTime = swPrompt.ElapsedMilliseconds,
                tokenUsage = new PromptTokenUsage()
                {
                    processingTokens = chatResponse.usage.completion_tokens,
                    promptTokens = chatResponse.usage.prompt_tokens,
                    totalTokens = chatResponse.usage.total_tokens
                }
            };

            //Add all choices
            foreach (var choice in chatResponse.choices)
            {
                promptResponse.values.Add(
                    new ResponseValue()
                    {
                        type = DataType.String,
                        value = choice.message.content
                    }
                );
            }
        }

        swPrompt.Stop();

        return promptResponse;
    }


    public async Task<IActionResult> ValidateModelAsync(string model)
    {
        _logger.LogInformation("ValidateModelAsync");

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Call the API and get the response
            HttpResponseMessage response = await client.GetAsync($"{MainUrl}models/{model}");

            var responseCode = response.StatusCode;

            if (responseCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unable to authenticate to OpenAI");
                return new BadRequestObjectResult(new
                    { Message = $"OpenAI: Unable to authenticate", ErrorCode = ModelConnectorErrors.InvalidApiKey });
            }

            if (responseCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("Invalid model name " + model);
                return new BadRequestObjectResult(new
                    { Message = $"OpenAI: Invalid model name", ErrorCode = ModelConnectorErrors.InvalidApiKey });
            }
        }

        _logger.LogInformation("OpenAI model valid");
        return new OkObjectResult(new { Message = $"Model valid" });
    }

    public async Task<List<List<float>>> GetEmbeddingsAsync(List<string> strings)
    {
        List<List<float>> pagesEmbeddings = new List<List<float>>();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create the request message
            OpenAiEmbeddings.EmbeddingRequest request = new OpenAiEmbeddings.EmbeddingRequest
            {
                input = strings,
                model = "text-embedding-ada-002"
            };

            string requestBody = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Call the API and get the response
            _logger.LogInformation("Calling OpenAI embeddings API {0}", $"{MainUrl}embeddings");
            HttpResponseMessage response = await client.PostAsync($"{MainUrl}embeddings", content);
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                _logger.LogError("Error while receiving response from OpenAI");
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogError("Error while receiving response from OpenAI, too many requests");
                // handle too many requests (billing, quota issues)
                throw new Exception("Error while receiving response from OpenAI, too many requests");
            }

            //response.EnsureSuccessStatusCode();
            string responseContent = await response.Content.ReadAsStringAsync();

            OpenAiEmbeddings.EmbeddingResponse? embeddingResponse =
                JsonSerializer.Deserialize<OpenAiEmbeddings.EmbeddingResponse>(responseContent);

            if (embeddingResponse == null)
            {
                _logger.LogError("Error while receiving response from OpenAI");
                throw new Exception("Error while receiving response from OpenAI");
            }

            // Handle the embedding response
            foreach (OpenAiEmbeddings.EmbeddingData data in embeddingResponse.data) //Each string
            {
                pagesEmbeddings.Add(data.embedding);
            }
        }

        _logger.LogInformation("OpenAI embeddings received");
        return pagesEmbeddings;
    }

    public async Task<PromptResponse> GenerateImageAsync(PromptRequest prompt, string model)
    {
        PromptResponse promptResponse = new PromptResponse();

        Stopwatch swPrompt = new Stopwatch();
        swPrompt.Start();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //Create the request message
            GenerateImageRequestMessage request = new GenerateImageRequestMessage
            {
                prompt = prompt.user,
                size = prompt.resolution,
                n = prompt.options,
                response_format = "url"
            };

            string requestBody = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            //Call the API and get the response
            HttpResponseMessage response = await client.PostAsync($"{MainUrl}images/generations", content);
            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();
            GenerateImageResponseMessage? generateResponse =
                JsonSerializer.Deserialize<GenerateImageResponseMessage>(responseContent);

            //Detect if b64_json OR url
            //If URL, download
            switch (request.response_format)
            {
                case "b64_json":
                    //Convert response to byte[]
                    foreach (var data in generateResponse.data)
                    {
                        byte[] imageRaw = Convert.FromBase64String(data.b64_json);

                        promptResponse.values.Add(
                            new ResponseValue()
                            {
                                type = DataType.Image,
                                format = "png",
                                valueRaw = imageRaw,
                                b64 = data.b64_json
                            }
                        );
                    }

                    break;
                case "url":
                    //Download to byte[]
                    foreach (var data in generateResponse.data)
                    {
                        HttpClient imageDownloader = new HttpClient();
                        byte[] imageRaw = await imageDownloader.GetByteArrayAsync(data.url);

                        promptResponse.values.Add(
                            new ResponseValue()
                            {
                                type = DataType.Image,
                                format = "png",
                                valueRaw = imageRaw,
                                b64 = Convert.ToBase64String(imageRaw)
                            }
                        );
                    }

                    break;
            }

            if (generateResponse == null)
            {
                throw new Exception("Error while receiving response from OpenAI");
            }

            //Fill Response
            promptResponse.elapsedProcessingTime = swPrompt.ElapsedMilliseconds;
        }

        swPrompt.Stop();

        return promptResponse;
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