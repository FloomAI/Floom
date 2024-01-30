using Microsoft.AspNetCore.Mvc;
using Floom.Auth;
using Floom.Data;
using Floom.Data.Entities;
using Floom.Services;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;
        private readonly IDataService _dataService;

        public DataController(
            ILogger<DataController> logger,
            IDataService dataService)
        {
            _logger = logger;
            _dataService = dataService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DataDto>>> GetAll()
        {
            var dataModels = await _dataService.GetAll();
            return Ok(dataModels);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DataDto>> GetById(string id)
        {
            var data = await _dataService.GetById(id);
            return Ok(data);
        }

        [HttpPost("Apply")]
        public async Task<IActionResult> Apply(DataDto dataDto)
        {
            if (dataDto.embeddings == null)
            {
                return new BadRequestObjectResult(new { Message = "Embeddings not found" });
            }

            if (dataDto.vectorStore == null)
            {
                return new BadRequestObjectResult(new { Message = "Vector Store not found" });
            }

            var result = await _dataService.PrepareApply(dataDto);

            if (result is BadRequestObjectResult)
            {
                return result;
            }

            return new OkObjectResult(new { Message = $"Data applied {dataDto.id}" });
        }
    }
}