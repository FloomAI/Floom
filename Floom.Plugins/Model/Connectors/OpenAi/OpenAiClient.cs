using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Floom.Base;
using Floom.Logs;
using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Floom.Plugins.Prompt.Context.Embeddings.OpenAi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Model.Connectors.OpenAi;

public class OpenAiClient : IModelConnectorClient
{
    private ILogger _logger;
    readonly string MainUrl = "https://api.openai.com/v1/";
    public string? ApiKey { get; set; }

    public OpenAiClient()
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
    }
    
    public async Task<FloomPromptResponse> GenerateTextAsync(FloomPromptRequest prompt, string model)
    {
        FloomPromptResponse promptResponse = new FloomPromptResponse();

        Stopwatch swPrompt = new Stopwatch();
        swPrompt.Start();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create the request message
            GenerateTextRequestBody generateTextRequestBody = new GenerateTextRequestBody
            {
                model = model,
                messages = new List<Message>(),
                temperature = 0.5
            };

            //Add messages

            //Add History (System+User+Assistant)

            //Add all messages
            foreach (FloomPromptMessage promptMessage in prompt.previousMessages)
            {
                generateTextRequestBody.messages.Add(new Message()
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
                    generateTextRequestBody.messages.Add(new Message()
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
                generateTextRequestBody.messages.Add(new Message()
                {
                    role = "user",
                    content = prompt.user
                });
            }

            string requestBody = JsonSerializer.Serialize(generateTextRequestBody);
            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Call the API and get the response
            _logger.LogInformation("Calling OpenAI API {0}", $"{MainUrl}chat/completions");
            HttpResponseMessage response = await client.PostAsync($"{MainUrl}chat/completions", content);
            
            var responseCode = response.StatusCode;

            if (responseCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unable to authenticate to OpenAI");
                
                return new FloomPromptResponse()
                {
                    success = false,
                    message = $"OpenAI: Unable to authenticate",
                    errorCode = ModelConnectorErrors.InvalidApiKey
                };
            }

            if (responseCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("Invalid model name " + model);
                return new FloomPromptResponse()
                {
                    success = false,
                    message = $"OpenAI: Invalid model name",
                    errorCode = ModelConnectorErrors.InvalidModelName
                };
            }
            
            string responseContent = await response.Content.ReadAsStringAsync();
            GenerateTextResponseBody? chatResponse =
                JsonSerializer.Deserialize<GenerateTextResponseBody>(responseContent);

            if (chatResponse == null)
            {
                throw new Exception("Error while receiving response from OpenAI");
            }

            //Fill Response
            promptResponse = new FloomPromptResponse()
            {
                success = true,
                elapsedProcessingTime = swPrompt.ElapsedMilliseconds,
                tokenUsage = new FloomPromptTokenUsage()
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

    public async Task<FloomOperationResult<List<List<float>>>> GetEmbeddingsAsync(List<string> strings, string model)
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
                model = model
            };

            string requestBody = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Call the API and get the response
            _logger.LogInformation("Calling OpenAI embeddings API {0}", $"{MainUrl}embeddings");
            HttpResponseMessage response = await client.PostAsync($"{MainUrl}embeddings", content);
            
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var errorMessage = "Error while receiving response from OpenAI, too many requests";
                _logger.LogError(errorMessage);
                return FloomOperationResult<List<List<float>>>.CreateFailure(errorMessage);
            }
            
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var errorMessage = "Unable to authenticate to OpenAI";
                _logger.LogError(errorMessage);
                return FloomOperationResult<List<List<float>>>.CreateFailure(errorMessage);
            }

            if (response.StatusCode == HttpStatusCode.InternalServerError || !response.IsSuccessStatusCode)
            {
                var errorMessage = $"Error while receiving response from OpenAI. Status Code: {response.StatusCode}";
                _logger.LogError(errorMessage);
                return FloomOperationResult<List<List<float>>>.CreateFailure(errorMessage);
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
        return FloomOperationResult<List<List<float>>>.CreateSuccessResult(pagesEmbeddings);
    }

    public async Task<FloomPromptResponse> GenerateImageAsync(FloomPromptRequest prompt, string model)
    {
        FloomPromptResponse promptResponse = new FloomPromptResponse();

        Stopwatch swPrompt = new Stopwatch();
        swPrompt.Start();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            //Create the request message
            GenerateImageRequestBody request = new GenerateImageRequestBody
            {
                prompt = prompt.user,
                size = "1024x1024",
                n = 1,
                response_format = "url"
            };

            string requestBody = JsonSerializer.Serialize(request);
            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            _logger.LogInformation("Calling OpenAI API {0}", $"{MainUrl}images/generations");
            //Call the API and get the response
            HttpResponseMessage response = await client.PostAsync($"{MainUrl}images/generations", content);

            var responseCode = response.StatusCode;

            if (responseCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unable to authenticate to OpenAI");
                
                return new FloomPromptResponse()
                {
                    success = false,
                    message = $"OpenAI: Unable to authenticate",
                    errorCode = ModelConnectorErrors.InvalidApiKey
                };
            }

            if (responseCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("Invalid model name " + model);
                return new FloomPromptResponse()
                {
                    success = false,
                    message = $"OpenAI: Invalid model name",
                    errorCode = ModelConnectorErrors.InvalidModelName
                };
            }
            string responseContent = await response.Content.ReadAsStringAsync();
            
            GenerateImageResponseBody? generateResponse =
                JsonSerializer.Deserialize<GenerateImageResponseBody>(responseContent);

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
            promptResponse.success = true;
            promptResponse.elapsedProcessingTime = swPrompt.ElapsedMilliseconds;
        }

        swPrompt.Stop();

        return promptResponse;
    }
    
    public async Task<FloomPromptResponse> GenerateTextToSpeechAsync(FloomPromptRequest prompt, string model, string voice)
    {
        FloomPromptResponse promptResponse = new FloomPromptResponse();

        Stopwatch swPrompt = new Stopwatch();
        swPrompt.Start();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Create the request message
            GenerateTextToSpeechRequestBody generateTextToSpeechRequestBody = new GenerateTextToSpeechRequestBody
            {
                model = model,
                voice = voice,
                input = prompt.user ?? string.Empty
            };
            

            string requestBody = JsonSerializer.Serialize(generateTextToSpeechRequestBody);
            StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Call the API and get the response
            _logger.LogInformation("Calling OpenAI API {0}", $"{MainUrl}audio/speech");
            HttpResponseMessage response = await client.PostAsync($"{MainUrl}audio/speech", content);

            var responseCode = response.StatusCode;

            if (responseCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unable to authenticate to OpenAI");
                
                return new FloomPromptResponse()
                {
                    success = false,
                    message = $"OpenAI: Unable to authenticate",
                    errorCode = ModelConnectorErrors.InvalidApiKey
                };
            }

            if (responseCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("Invalid model name " + model);
                return new FloomPromptResponse()
                {
                    success = false,
                    message = $"OpenAI: Invalid model name",
                    errorCode = ModelConnectorErrors.InvalidModelName
                };
            }
            // Handling binary data
            var responseBytes = await response.Content.ReadAsByteArrayAsync();

            // For example, converting to Base64 if you need to handle it as a string.
            var base64Audio = Convert.ToBase64String(responseBytes);

            promptResponse = new FloomPromptResponse()
            {
                success = true,
                elapsedProcessingTime = swPrompt.ElapsedMilliseconds,
                values = new List<ResponseValue>
                {
                    new()
                    {
                        type = DataType.Audio,
                        b64 = base64Audio
                    }
                }
            };
        }
        swPrompt.Stop();

        return promptResponse;
    }
    
    
    public async Task<FloomPromptResponse> GenerateSpeechToTextAsync(byte[] audioBytes, string model)
    {
        FloomPromptResponse promptResponse = new FloomPromptResponse();
        Stopwatch swPrompt = new Stopwatch();
        swPrompt.Start();

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
        
            using (var content = new MultipartFormDataContent())
            {
                // Add the audio file
                var audioContent = new ByteArrayContent(audioBytes);
                audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mp3");
                content.Add(audioContent, "file", "audio.mp3");
            
                // Add the model field
                content.Add(new StringContent(model), "model");

                // Log the request (ensure your logger is configured to handle this appropriately)
                _logger.LogInformation("Calling OpenAI API for speech-to-text with model {0}", model);

                // Call the API and get the response
                var response = await client.PostAsync($"{MainUrl}audio/transcriptions", content);

                var responseCode = response.StatusCode;

                if (responseCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogError("Unable to authenticate to OpenAI");
                
                    return new FloomPromptResponse()
                    {
                        success = false,
                        message = $"OpenAI: Unable to authenticate",
                        errorCode = ModelConnectorErrors.InvalidApiKey
                    };
                }

                if (responseCode == HttpStatusCode.NotFound)
                {
                    _logger.LogError("Invalid model name " + model);
                    return new FloomPromptResponse()
                    {
                        success = false,
                        message = $"OpenAI: Invalid model name",
                        errorCode = ModelConnectorErrors.InvalidModelName
                    };
                }
                
                // Assuming the response is JSON (adjust accordingly)
                var responseString = await response.Content.ReadAsStringAsync();
                var transcriptionResponse = JsonSerializer.Deserialize<GenerateSpeechResponseBody>(responseString);

                //Fill Response
                promptResponse = new FloomPromptResponse()
                {
                    success = true,
                    elapsedProcessingTime = swPrompt.ElapsedMilliseconds,
                };

                promptResponse.values.Add(
                    new ResponseValue()
                    {
                        type = DataType.String,
                        value = transcriptionResponse.text
                    }
                );
            }
        }

        swPrompt.Stop();
        return promptResponse;
    }

}

public class GenerateImageRequestBody
{
    public string? prompt { get; set; }
    public string? size { get; set; }
    public uint? n { get; set; } //Options

    public string response_format { get; set; } = "b64_json";
}

public class GenerateTextRequestBody
{
    public string model { get; set; }
    public List<Message> messages { get; set; }
    public double temperature { get; set; }
}

public class GenerateTextToSpeechRequestBody
{
    public string model { get; set; }
    public string input { get; set; }
    public string voice { get; set; }
}

public class GenerateTextToSpeechResponseBody
{
    public string model { get; set; }
    public string input { get; set; }
    public string voice { get; set; }
}

public class Message
{
    public string? role { get; set; }
    public string? content { get; set; }
}

public class GenerateTextResponseBody
{
    public string? id { get; set; }

    [JsonPropertyName("object")] public string? Object { get; set; }

    public long created { get; set; }
    public string? model { get; set; }
    public Usage? usage { get; set; }
    public List<Choice>? choices { get; set; }
}


public class GenerateImageResponseBody
{
    public long created { get; set; }
    public List<ImageData>? data { get; set; }
}

public class GenerateSpeechResponseBody
{
    public string text { get; set; }
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