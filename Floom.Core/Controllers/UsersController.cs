using Floom.Auth;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers;

[ApiController]
[Route("/v{version:apiVersion}/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUsersService _service;

    public UsersController(IUsersService service)
    {
        _service = service;
    }

    [HttpPost("Register")]
    public async Task<IActionResult> Register()
    {
        var apiKey = await _service.RegisterGuestUserAsync();
        return Ok(apiKey);
    }
}