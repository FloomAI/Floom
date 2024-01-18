using Azure;
using Azure.AI.OpenAI;
//using Azure.AI.OpenAI2;
using Floom.Misc;
using Microsoft.AspNetCore.Mvc.Formatters;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static iText.Svg.SvgConstants;

namespace Floom.Managers.AIProviders.Engines.OpenAI2
{
    public class OpenAI2/* : AIProvider*/
    {
        string MainUrl = "https://api.openai.com/v1/";
        public string ApiKey = string.Empty;

        public OpenAI2(string ApiKey)
        {
            this.ApiKey = ApiKey;
        }

        public async Task<List<List<float>>> GetEmbeddingsAsync(List<string> strings)
        {
            List<List<float>> pagesEmbeddings = new List<List<float>>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.ApiKey);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Create the request message
                EmbeddingRequest request = new EmbeddingRequest
                {
                    input = strings,
                    model = "text-embedding-ada-002"
                };

                string requestBody = System.Text.Json.JsonSerializer.Serialize(request);
                StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Call the API and get the response
                HttpResponseMessage response = await client.PostAsync($"{MainUrl}embeddings", content);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                EmbeddingResponse? embeddingResponse = System.Text.Json.JsonSerializer.Deserialize<EmbeddingResponse>(responseContent);

                if (embeddingResponse == null)
                {
                    throw new Exception("Error while receiving response from OpenAI");
                }

                // Handle the embedding response
                foreach (EmbeddingData data in embeddingResponse.data) //Each string
                {
                    pagesEmbeddings.Add(data.embedding);
                }
            }

            return pagesEmbeddings;
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
                    generateTextRequestMessage.messages.Add(new Message()
                    {
                        role = "system",
                        content = prompt.system
                    });
                }
                //dd
                //Add User
                generateTextRequestMessage.messages.Add(new Message()
                {
                    role = "user",
                    content = prompt.user
                });

                string requestBody = System.Text.Json.JsonSerializer.Serialize(generateTextRequestMessage);
                StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                // Call the API and get the response
                HttpResponseMessage response = await client.PostAsync($"{MainUrl}chat/completions", content);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                GenerateTextResponseMessage? chatResponse = System.Text.Json.JsonSerializer.Deserialize<GenerateTextResponseMessage>(responseContent);

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
                        new Models.Other.Floom.ResponseValue()
                        {
                            type = Models.Other.Floom.DataType.String,
                            value = choice.message.content
                        }
                    );
                }
            }

            swPrompt.Stop();

            return promptResponse;
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

                string requestBody = System.Text.Json.JsonSerializer.Serialize(request);
                StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/json");

                //Call the API and get the response
                HttpResponseMessage response = await client.PostAsync($"{MainUrl}images/generations", content);
                response.EnsureSuccessStatusCode();

                string responseContent = await response.Content.ReadAsStringAsync();
                GenerateImageResponseMessage? generateResponse = System.Text.Json.JsonSerializer.Deserialize<GenerateImageResponseMessage>(responseContent);

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
                                new Models.Other.Floom.ResponseValue()
                                {
                                    type = Models.Other.Floom.DataType.Image,
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
                             new Models.Other.Floom.ResponseValue()
                             {
                                 type = Models.Other.Floom.DataType.Image,
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
        public string prompt { get; set; }
        public string size { get; set; }
        public uint n { get; set; } //Options

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
        public string role { get; set; }
        public string content { get; set; }
    }

    public class GenerateTextResponseMessage
    {
        public string id { get; set; }

        [JsonPropertyName("object")]
        public string Object { get; set; }

        public long created { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
        public List<Choice> choices { get; set; }
    }


    public class GenerateImageResponseMessage
    {
        public long created { get; set; }
        public List<ImageData> data { get; set; }
    }

    public class ImageData
    {
        public string url { get; set; }
        public string b64_json { get; set; }
    }

    public class Usage
    {
        public int prompt_tokens { get; set; }
        public int completion_tokens { get; set; }
        public int total_tokens { get; set; }
    }

    public class Choice
    {
        public Message message { get; set; }
        public string finish_reason { get; set; }
        public int index { get; set; }
    }

    public class EmbeddingResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; }

        public List<EmbeddingData> data { get; set; }
        public string model { get; set; }
        public Usage usage { get; set; }
    }

    public class EmbeddingData
    {
        [JsonPropertyName("object")]
        public string Object { get; set; }
        public List<float> embedding { get; set; }
        public int index { get; set; }
    }

    public class EmbeddingRequest
    {
        public List<string> input { get; set; }
        public string model { get; set; }
    }
}
