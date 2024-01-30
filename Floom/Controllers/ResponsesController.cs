using Floom.Auth;
using Floom.Entities.Response;
using Floom.Models;
using Floom.Services;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class ResponsesController : ControllerBase
    {
        private readonly IResponsesService _service;
        private readonly ILogger<ResponsesController> _logger;

        public ResponsesController(
            ILogger<ResponsesController> logger,
            IResponsesService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResponseDtoV1>>> Get()
        {
            var responses = await _service.GetAll();
            return Ok(responses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseDtoV1>> GetById(string id)
        {
            var model = await _service.GetById(id);
            return model == null ? Ok(new { }) : Ok(model);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<ResponseDtoV1>> Apply(ResponseDtoV1 responseDto)
        {
            await _service.Apply(responseDto);
            return new OkObjectResult(new { Message = $"Response applied {responseDto.id}" });
        }
    }
}