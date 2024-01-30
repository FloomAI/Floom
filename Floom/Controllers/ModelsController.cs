using Floom.Auth;
using Floom.Entities.Model;
using Floom.Services;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class ModelsController : ControllerBase
    {
        private readonly IModelsService _modelsService;
        private readonly ILogger<ModelsController> _logger;

        public ModelsController(
            ILogger<ModelsController> logger,
            IModelsService modelsService
        )
        {
            _logger = logger;
            _modelsService = modelsService;
        }

        [HttpGet]
        [Produces("application/json")]
        public async Task<ActionResult<IEnumerable<ModelDtoV1>>> Get()
        {
            var models = await _modelsService.GetAll();
            return Ok(models);
        }

        [HttpGet("{id}")]
        [Produces("application/json")]
        public async Task<ActionResult<ModelDtoV1>> GetById(string id)
        {
            var model = await _modelsService.GetById(id);
            return model == null ? Ok(new { }) : Ok(model);
        }

        [HttpPost("Apply")]
        public async Task<IActionResult> Apply(ModelDtoV1 modelDto)
        {
            var modelValid = await _modelsService.Validate(modelDto);

            if (modelValid is not OkObjectResult)
            {
                return modelValid;
            }

            await _modelsService.Apply(modelDto);

            return new OkObjectResult(new { Message = $"Model applied {modelDto.id}" });
        }
    }
}