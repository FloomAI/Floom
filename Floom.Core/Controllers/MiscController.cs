using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    public class MiscController : ControllerBase
    {
        private readonly ILogger<MiscController> _logger;

        public MiscController(
            ILogger<MiscController> logger)
        {
            _logger = logger;
        }

        [HttpGet("Health")]
        public async Task<IActionResult> Health()
        {
            return Ok("Healthy");
        }
    }
}