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


    [HttpGet("google-login")]
    public IActionResult Login()
    {
        Console.WriteLine("Google login");
        var properties = new AuthenticationProperties 
        { 
            RedirectUri = "https://api.floom.ai/v1/account/google-response" 
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
        Console.WriteLine("Google response");
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return BadRequest("Google authentication failed");

        var emailClaim = result.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        
        if (string.IsNullOrEmpty(emailClaim))
            return BadRequest("No email claim found");

        var response = await _service.RegisterOrLoginUserAsync("google", emailClaim);

        // Generate a session token (for simplicity, using a GUID here)
        var sessionToken = response.ApiKey;

        // Set cookies
        var secureCookieOptions = new CookieOptions { Secure = true, SameSite = SameSiteMode.None };

        HttpContext.Response.Cookies.Append("SessionToken", response.ApiKey, secureCookieOptions);
        HttpContext.Response.Cookies.Append("Username", response.Username, secureCookieOptions);
        HttpContext.Response.Cookies.Append("Nickname", response.Nickname, secureCookieOptions);

        // Return JSON response with session token
        return Redirect("https://www.floom.ai/");
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
                new KeyValuePair<string, string>("code", request.Code)
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
}