using Floom.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Cors;
using Floom.Utils;
namespace Floom.Controllers;

[ApiController]
[Route("/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[EnableCors]
public class AccountController : ControllerBase
{
    private readonly IUsersService _service;

    public AccountController(IUsersService service)
    {
        _service = service;
    }

    [HttpPost("RegisterGuest")]
    public async Task<IActionResult> Register()
    {
        var apiKey = await _service.RegisterGuestUserAsync();
        return Ok(apiKey);
    }


    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleCallback([FromBody] GoogleAuthRequest request)
    {
        var clientId = Environment.GetEnvironmentVariable("FLOOM_GOOGLE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("FLOOM_GOOGLE_CLIENT_SECRET");

        using (var httpClient = new HttpClient())
        {
            Console.WriteLine("Starting token request to Google.");
            var tokenResponse = await httpClient.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", request.Code),
                new KeyValuePair<string, string>("redirect_uri", "https://www.floom.ai"), // Your redirect URI
                new KeyValuePair<string, string>("grant_type", "authorization_code")
            }));

            var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();
            Console.WriteLine("Token response received: " + tokenResponseBody);
            tokenResponse.EnsureSuccessStatusCode();

            var tokenData = JObject.Parse(tokenResponseBody);
            var accessToken = tokenData["access_token"].ToString();
            Console.WriteLine("Access token: " + accessToken);

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("floom.ai");

            Console.WriteLine("Starting user info request to Google.");
            var userResponse = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            var userResponseBody = await userResponse.Content.ReadAsStringAsync();
            Console.WriteLine("User info response received: " + userResponseBody);

            userResponse.EnsureSuccessStatusCode();
            var user = JObject.Parse(userResponseBody);

            var email = user["email"].ToString();
            Console.WriteLine("User email: " + email);

            var response = await _service.RegisterOrLoginUserAsync("google", email);

            var sessionToken = response.ApiKey;
            Console.WriteLine("Session token: " + sessionToken);

            return Ok(new { sessionToken });
        }

    }

    [HttpPost("github-login")]
    public async Task<IActionResult> GitHubLogin([FromBody] GitHubAuthRequest request)
    {
        var clientId = Environment.GetEnvironmentVariable("FLOOM_GITHUB_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("FLOOM_GITHUB_CLIENT_SECRET");

        using (var httpClient = new HttpClient())
        {
            var tokenResponse = await httpClient.PostAsync("https://github.com/login/oauth/access_token", new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", request.Code),
                new KeyValuePair<string, string>("state", request.State)
            }));

            var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

            tokenResponse.EnsureSuccessStatusCode();
            var queryParams = System.Web.HttpUtility.ParseQueryString(tokenResponseBody);
            var accessToken = queryParams["access_token"];

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("floom.ai");

            var userResponse = await httpClient.GetAsync("https://api.github.com/user");

            var userResponseBody = await userResponse.Content.ReadAsStringAsync();

            userResponse.EnsureSuccessStatusCode();
            var user = JObject.Parse(userResponseBody);

            var emailResponse = await httpClient.GetAsync("https://api.github.com/user/emails");

            var emailResponseBody = await emailResponse.Content.ReadAsStringAsync();

            emailResponse.EnsureSuccessStatusCode();
            var emails = JArray.Parse(emailResponseBody);

            // Process user and email data as needed
            // Save to your database and generate session token or JWT
            var response = await _service.RegisterOrLoginUserAsync("github", emails.First().ToString());

            Console.WriteLine($"Email response body: {emails.First().ToString()}");

            var sessionToken = response.ApiKey;

            return Ok(new { sessionToken });
        }
    }

    [HttpGet("logout")]
    [ApiKeyAuthorization]
    public async Task<IActionResult> Logout()
    {
        var apiKey = HttpContextHelper.GetApiKeyFromHttpContext();
        Console.WriteLine($"Logging out user with API key: {apiKey}");
        await _service.LogoutUserByApiKeyAsync(apiKey);
        return Ok("Logged out");
    }
}

public class GitHubAuthRequest
{
    public string Code { get; set; }
    public string State { get; set; }
}

public class GoogleAuthRequest
{
    public string Code { get; set; }
    public string State { get; set; }
}