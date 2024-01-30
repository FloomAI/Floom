using Floom.Auth;
using Floom.Models;
using Floom.Services;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class PromptsController : ControllerBase
    {
        private readonly IPromptsService _service;
        private readonly ILogger<PromptsController> _logger;

        public PromptsController(
            ILogger<PromptsController> logger,
            IPromptsService service
        )
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PromptDtoV1>>> Get()
        {
            var prompts = await _service.GetAll();
            return Ok(prompts);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PromptDtoV1>> GetById(string id)
        {
            var model = await _service.GetById(id);
            return model == null ? Ok(new { }) : Ok(model);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<PromptDtoV1>> Apply(PromptDtoV1 promptDto)
        {
            await _service.Apply(promptDto);
            return new OkObjectResult(new { Message = $"Prompt applied {promptDto.id}" });
        }
    }
}