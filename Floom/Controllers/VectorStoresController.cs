using Floom.Auth;
using Floom.Entities.VectorStore;
using Floom.VectorStores;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class VectorStoresController : ControllerBase
    {
        private readonly IVectorStoresService _service;
        private readonly ILogger<VectorStoresController> _logger;

        public VectorStoresController(
            ILogger<VectorStoresController> logger,
            IVectorStoresService service)
        {
            _logger = logger;
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VectorStoreDtoV1>>> Get()
        {
            var vectorStores = await _service.GetAll();
            return Ok(vectorStores);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VectorStoreDtoV1>> GetById(string id)
        {
            var vectorStore = await _service.GetById(id);
            return Ok(vectorStore);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<VectorStoreDtoV1>> Apply(VectorStoreDtoV1 dto)
        {
            await _service.Apply(dto.ToModel());
            return new OkObjectResult(new { Message = $"Vector Store applied {dto.id}" });
        }
    }
}