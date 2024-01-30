using Floom.Auth;
using Floom.Embeddings;
using Floom.Embeddings.Entities;
using Floom.Entities.Embeddings;
using Floom.Services;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class EmbeddingsController : ControllerBase
    {
        private readonly IEmbeddingsService _service;
        private readonly ILogger<EmbeddingsController> _logger;

        public EmbeddingsController(
            ILogger<EmbeddingsController> logger,
            IEmbeddingsService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<EmbeddingsModel>>> Get()
        {
            var embeddings = await _service.GetAll();
            return Ok(embeddings);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<EmbeddingsModel>> GetById(string id)
        {
            var embeddings = await _service.GetById(id);
            return Ok(embeddings);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult> Apply(EmbeddingsDtoV1 dto)
        {
            await _service.Apply(dto);
            return new OkObjectResult(new { Message = $"Embeddings applied {dto.id}" });
        }
    }
}