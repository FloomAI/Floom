using Floom.Auth;
using Floom.Pipeline;
using Floom.Pipeline.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers;

[ApiController]
[Route("/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiKeyAuthorization]
public class PipelinesController : ControllerBase
{
    private readonly ILogger<PipelinesController> _logger;
    private readonly IPipelinesService _service;

    public PipelinesController(
        ILogger<PipelinesController> logger,
        IPipelinesService service)

    {
        _logger = logger;
        _service = service;
    }
        
    [HttpPost("Commit")]
    public async Task<IActionResult?> Commit(PipelineDto? pipelineDto)
    {
        _logger.LogInformation("Pipelines/Commit Invoked");
            
        if (pipelineDto == null)
        {
            return GenerateBadRequestResponse();
        }

        await _service.Commit(pipelineDto);
            
        return new OkResult();
    }

    [HttpPost("Run")]
    [Consumes("application/json")]
    public async Task<IActionResult> Run(RunFloomPipelineRequest? floomRequest)
    {
        return await RunCommon(floomRequest);
    }

    [HttpPost("Run")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> RunForm([FromForm] RunFloomPipelineRequest? floomRequest)
    {
        return await RunCommon(floomRequest);
    }

    private async Task<IActionResult> RunCommon(RunFloomPipelineRequest? floomRequest)
    {
        if (floomRequest == null)
        {
            return GenerateBadRequestResponse();
        }

        var response = await _service.Run(floomRequest);

        if (response == null)
        {
            // Handle null response appropriately
            return NotFound();
        }

        if (response is FloomPipelineErrorResponse)
        {
            var baseResponse = response as FloomPipelineErrorResponse;
            if (baseResponse?.success == false)
            {
                return GenerateErrorResponse(baseResponse);
            }
        }

        return Ok(response);
    }

    private IActionResult GenerateBadRequestResponse()
    {
        var errorResponse = new
        {
            Status = 400,
            Title = "Bad Request",
            Detail = "Invalid request: The request body is missing or incorrectly formatted."
        };
        return BadRequest(errorResponse);
    }
    
    private IActionResult GenerateErrorResponse(FloomPipelineErrorResponse response)
    {
        var errorResponse = new
        {
            Status = response.statusCode,
            Title = "Bad Request",
            Detail = response.message
        };
        return BadRequest(errorResponse);
    }
}