using Floom.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Floom.Controllers;

[ApiController]
[Route("/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
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
        var properties = new AuthenticationProperties { RedirectUri = "/v1/account/google-response" };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-response")]
    public async Task<IActionResult> GoogleResponse()
    {
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
        HttpContext.Response.Cookies.Append("SessionToken", response.ApiKey, new CookieOptions { });
        HttpContext.Response.Cookies.Append("Username", response.Username, new CookieOptions { });
        HttpContext.Response.Cookies.Append("Nickname", response.Nickname, new CookieOptions { });

        // Return JSON response with session token
        return Redirect("http://localhost:3000");
    }

    [HttpGet("github-login")]
    public IActionResult GitHubLogin()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/v1/account/github-response" };
        return Challenge(properties, "GitHub");
    }

    [HttpGet("github-response")]
    public async Task<IActionResult> GitHubResponse()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!result.Succeeded)
            return BadRequest("GitHub authentication failed");

        var emailClaim = result.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(emailClaim))
            return BadRequest("No email claim found");

        var response = await _service.RegisterOrLoginUserAsync("github", emailClaim);

        // Set cookies without HttpOnly flag so they can be accessed by JavaScript
        HttpContext.Response.Cookies.Append("SessionToken", response.ApiKey, new CookieOptions { Secure = true });
        HttpContext.Response.Cookies.Append("Username", response.Username, new CookieOptions { Secure = true });
        HttpContext.Response.Cookies.Append("Nickname", response.Nickname, new CookieOptions { Secure = true });

        return Redirect("http://localhost:3000");
    }


    [HttpGet("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Response.Cookies.Delete("SessionToken");
        HttpContext.Response.Cookies.Delete("Username");
        HttpContext.Response.Cookies.Delete("Nickname");
        return Ok("Logged out");
    }
}