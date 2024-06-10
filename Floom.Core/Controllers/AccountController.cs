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

        // Generate a session token (for simplicity, using a GUID here)
        var sessionToken = Guid.NewGuid().ToString();

        // Retrieve user email from claims
        var emailClaim = result.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        // Set session token as a cookie
        HttpContext.Response.Cookies.Append("SessionToken", sessionToken);

        // Return JSON response with session token
        return Redirect("http://localhost:3000");
    }

    [HttpGet("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Response.Cookies.Delete("SessionToken");
        return Ok("Logged out");
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

        var sessionToken = Guid.NewGuid().ToString();
        var emailClaim = result.Principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        HttpContext.Response.Cookies.Append("SessionToken", sessionToken);
        return Redirect("http://localhost:3000");
    }
}
