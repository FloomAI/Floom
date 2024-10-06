using Floom.Auth;
using Floom.Functions;
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
                var functionName = await _functionsService.DeployFunctionAsync(filePath, userId);

                // return JSON with message, function name, which says, function X deployed successfully
                return Ok(new { message = $"Function {functionName} deployed successfully" });
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

        [HttpGet("list")]
        public async Task<IActionResult> ListFunctions()
        {
            var userId = HttpContextHelper.GetUserIdFromHttpContext();
            var functions = await _functionsService.ListFunctionsAsync(userId);
            return Ok(functions);
        }

        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> ListPublicFeaturedFunctions()
        {
            var publicFeaturedFunctions = await _functionsService.ListPublicFeaturedFunctionsAsync();
            return Ok(publicFeaturedFunctions);
        }

        [HttpPost("addRoles")]
        public async Task<IActionResult> AddRolesToFunction([FromBody] ModifyRolesRequest request)
        {
            try
            {
                var userId = HttpContextHelper.GetUserIdFromHttpContext();
                await _functionsService.AddRolesToFunctionAsync(request.functionName, request.userId, userId);
                return Ok(new { message = $"Roles 'Public' and 'Featured' added to function {request.functionName}." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPost("removeRoles")]
        public async Task<IActionResult> RemoveRolesFromFunction([FromBody] ModifyRolesRequest request)
        {
            try
            {
                var userId = HttpContextHelper.GetUserIdFromHttpContext();
                await _functionsService.RemoveRolesToFunctionAsync(request.functionName, request.userId, userId);
                return Ok(new { message = $"Roles 'Public' and 'Featured' removed from function {request.functionName}." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
}

public class ModifyRolesRequest
{
    public string functionName { get; set; }
    public string userId { get; set; }
}

public class RunFunctionRequest
{
    public string function { get; set; }
    public string prompt { get; set; }
}