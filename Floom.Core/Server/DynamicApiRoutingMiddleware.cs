using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using Floom.Pipeline.Entities.Dtos;

namespace Floom.Server;

public class DynamicApiRoutingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public DynamicApiRoutingMiddleware(RequestDelegate next, ILogger<DynamicApiRoutingMiddleware> logger, HttpClient httpClient)
    {
        _next = next;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        _logger.LogInformation($"DynamicApiRoutingMiddleware invoked for host: {host}");
        if (Regex.IsMatch(host, @"^.+\.pipeline\.floom\.ai$"))
        {
            await ModifyAndForwardRequestAsync(context);
        }
        else
        {
            // Not a matching request, proceed to the next middleware
            await _next(context);
        }
    }

    private async Task ModifyAndForwardRequestAsync(HttpContext context)
    {
        // Extract the pipelineId and username from the host
        var regexInput = context.Request.Host.Host;
        var match = Regex.Match(regexInput, @"^(?<pipelineId>.+)\.pipeline\.floom\.ai$");
        if (!match.Success)
        {
            _logger.LogError("Failed to parse hostname for routing.");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }
        var pipelineId = match.Groups["pipelineId"].Value;

        var lastIndex = pipelineId.LastIndexOf('-');

        // Split into the actual pipelineId and username based on the last '-'
        var actualPipelineId = pipelineId.Substring(0, lastIndex);
        var username = pipelineId.Substring(lastIndex + 1);
            
        // Read the incoming request body
        string requestBody;
        context.Request.EnableBuffering();
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, true, 1024, true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset the stream for any downstream middleware
        }
        var jsonDocument = JsonDocument.Parse(requestBody);
        string? inputString = null;
        if (jsonDocument.RootElement.TryGetProperty("prompt", out var inputElement))
        {
            inputString = inputElement.GetString();
        }
        object? responseTypeJson = null;
        if (jsonDocument.RootElement.TryGetProperty("responseType", out var responseTypeElement))
        {
            responseTypeJson = responseTypeElement;
        }
        // Construct the new request URL and body
        var newUrl = "http://localhost:4050/v1/pipelines/run";
        // Construct the payload object
        var payload = new RunFloomPipelineRequest()
        {
            pipelineId = actualPipelineId,
            username = username,
            prompt = inputString
        };
        
        if (responseTypeJson != null)
        {
            payload.responseType = responseTypeJson;
        }
        
        try
        {
            var newBody = JsonSerializer.Serialize(payload);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, newUrl)
            {
                Content = new StringContent(newBody, Encoding.UTF8, "application/json")
            };

            // Copy headers from the incoming request to the outgoing request
            foreach (var header in context.Request.Headers)
            {
                // Check for headers that should not be copied directly
                if (!header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) &&
                    !header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // Forward the modified request
            var response = await _httpClient.SendAsync(requestMessage);

            context.Response.StatusCode = (int)response.StatusCode;

            // Explicitly copy the Content-Type header (and any other headers you wish to preserve)
            if (response.Content.Headers.ContentType != null)
            {
                context.Response.ContentType = response.Content.Headers.ContentType.ToString();
            }

            // Optionally, forward other specific headers here
            foreach (var header in response.Headers)
            {
                // Avoid copying restricted headers like 'Transfer-Encoding'
                if (!context.Response.Headers.ContainsKey(header.Key) && 
                    !header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // Stream the proxied response content back to the original caller
            using (var responseStream = await response.Content.ReadAsStreamAsync())
            {
                await responseStream.CopyToAsync(context.Response.Body);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"Error forwarding request: {ex.Message}");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}