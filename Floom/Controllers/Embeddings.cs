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
    public class EmbeddingsController : ControllerBase
    {
        private readonly ILogger<EmbeddingsController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        public EmbeddingsController(
            ILogger<EmbeddingsController> logger,
            IDatabaseService databaseService,
            IDynamicHelpersService dynamicHelpersService,
            IDeserializer yamlDeserializer,
            ISerializer yamlSerializer
        )
        {
            _db = databaseService;
            _dynamicHelpers = new DynamicHelpersService(_db);
        }

        [HttpGet]
        [Produces("text/yaml")]
        public async Task<ActionResult<IEnumerable<EmbeddingsDtoV1>>> Get()
        {
            var embeddings = await _db.Embeddings.Find(_ => true).ToListAsync();
            var embeddingsDtos = embeddings.Select(EmbeddingsDtoV1.FromEmbeddings);
            var yamlDatas = _yamlSerializer.Serialize(embeddingsDtos);
            return Content(yamlDatas);
        }

        [HttpGet("{id}")]
        [Produces("text/yaml")]
        public async Task<ActionResult<EmbeddingsDtoV1>> GetById(string id)
        {
            var filter = Builders<Embeddings>.Filter.Eq("Id", id);
            var embeddingsProvider = await _db.Embeddings.Find(filter).FirstOrDefaultAsync();
            if (embeddingsProvider == null)
                return NotFound();

            var embeddingsProviderDto = EmbeddingsDtoV1.FromEmbeddings(embeddingsProvider);
            var yamlData = _yamlSerializer.Serialize(embeddingsProviderDto);
            return Content(yamlData);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<EmbeddingsDtoV1>> Apply(EmbeddingsDtoV1 embeddingsDto)
        {
            var validationResult = await new EmbeddingsDtoV1Validator().ValidateAsync(embeddingsDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            Embeddings embeddings = embeddingsDto.ToEmbeddings();

            embeddings.createdAt = DateTime.UtcNow;
            embeddings.createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null;

            #region Delete (if already exists)

            var epFilter = Builders<Models.Embeddings>.Filter.Eq(f => f.name, embeddingsDto.id);
            var existingEmbeddings = await _db.Embeddings.Find(epFilter).FirstOrDefaultAsync();

            if (existingEmbeddings != null)
            {
                //Delete Embeddings
                await _db.Embeddings.DeleteOneAsync(epFilter);
            }

            #endregion

            await _db.Embeddings.InsertOneAsync(embeddings);

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