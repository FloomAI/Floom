using Floom.Helpers;
using Floom.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SharpCompress.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class VectorStoresController : ControllerBase
    {
        private readonly ILogger<VectorStoresController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        public VectorStoresController(
            ILogger<VectorStoresController> logger,
            IDatabaseService databaseService,
            IDynamicHelpersService dynamicHelpersService,
            IDeserializer yamlDeserializer,
            ISerializer yamlSerializer
        )
        {
            _db = databaseService;
            _dynamicHelpers = dynamicHelpersService;
        }

        [HttpGet]
        [Produces("text/yaml")]
        public async Task<ActionResult<IEnumerable<VectorStoreDtoV1>>> Get()
        {
            var vectorStores = await _db.VectorStores.Find(_ => true).ToListAsync();
            var vectorStoresDtos = vectorStores.Select(VectorStoreDtoV1.FromVectorStore);
            var yamlDatas = _yamlSerializer.Serialize(vectorStoresDtos);
            return Content(yamlDatas);
        }

        [HttpGet("{id}")]
        [Produces("text/yaml")]
        public async Task<ActionResult<VectorStoreDtoV1>> GetById(string id)
        {
            var filter = Builders<Models.VectorStore>.Filter.Eq("Id", id);
            var vectorStore = await _db.VectorStores.Find(filter).FirstOrDefaultAsync();
            if (vectorStore == null)
                return NotFound();

            var vectorStoreDto = VectorStoreDtoV1.FromVectorStore(vectorStore);
            var yamlData = _yamlSerializer.Serialize(vectorStoreDto);
            return Content(yamlData);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<VectorStoreDtoV1>> Apply(VectorStoreDtoV1 vectorStoreDto)
        {
            var validationResult = await new VectorStoreDtoV1Validator().ValidateAsync(vectorStoreDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            Models.VectorStore vectorStore = vectorStoreDto.ToVectorStore();

            vectorStore.createdAt = DateTime.UtcNow;
            vectorStore.createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null;

            #region Delete (if already exists)

            var vsFilter = Builders<Models.VectorStore>.Filter.Eq(f => f.name, vectorStoreDto.id);
            var existingVS = await _db.VectorStores.Find(vsFilter).FirstOrDefaultAsync();

            if (existingVS != null)
            {
                //Delete VectorStore
                await _db.VectorStores.DeleteOneAsync(vsFilter);
            }

            #endregion

            await _db.VectorStores.InsertOneAsync(vectorStore);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            //var filter = Builders<Data>.Filter.Eq("Id", id);
            //var result = await _dataSetsCollection.DeleteOneAsync(filter);
            //if (result.DeletedCount == 0)
            //    return NotFound();

            return Ok();
        }
    }
}
