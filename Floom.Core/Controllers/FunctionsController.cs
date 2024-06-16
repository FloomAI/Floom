using Floom.Auth;
using Floom.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers;

[ApiController]
[Route("/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiKeyAuthorization]
public class FunctionsController : ControllerBase
{
        private readonly IFunctionsService _functionsService;

        public FunctionsController(IFunctionsService functionsService)
        {
            _functionsService = functionsService;
        }

        [HttpPost("deploy")]
        public async Task<IActionResult> DeployFunction([FromForm] IFormFile file)
        {
            // 1. Save the file to disk
            var filePath = Path.GetTempFileName();
            try
            {
                using (var stream = System.IO.File.Create(filePath))
                {
                    await file.CopyToAsync(stream);
                }

                var userId = HttpContextHelper.GetUserIdFromHttpContext();

                // 2. Deploy the function
                var functionUrl = await _functionsService.DeployFunctionAsync(filePath, userId);

                // 3. Return the URL
                return Ok(functionUrl);
            }
            finally
            {
                // Ensure the temporary file is deleted
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }


        [HttpPost("run")]
        public async Task<IActionResult> RunFunction([FromBody] RunFunctionRequest request)
        {
        var userId = HttpContextHelper.GetUserIdFromHttpContext();
        var result = await _functionsService.RunFunctionAsync(userId, request.function, request.prompt);
        return Ok(result);
        }   
}

public class RunFunctionRequest
{
    public string function { get; set; }
    public string prompt { get; set; }
}