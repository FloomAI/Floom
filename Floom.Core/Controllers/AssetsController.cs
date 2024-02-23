using Floom.Assets;
using Floom.Data;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class AssetsController : ControllerBase
    {
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(ILogger<AssetsController> logger)
        {
            _logger = logger;
        }
        
        [HttpPost]
        public async Task<IActionResult> Create()
        {
            try
            {
                var file = Request.Form.Files.FirstOrDefault(); // Get the uploaded file from the request
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No asset was provided" });
                }

                var fileId = await FloomAssetsRepository.Instance.CreateAsset(file);
                return Ok(new { fileId });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error occurred while creating asset.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred while creating asset." });
            }
        }
    }
}