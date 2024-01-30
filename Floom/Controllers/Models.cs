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
    public class ModelsController : ControllerBase
    {
        private readonly ILogger<ModelsController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        public ModelsController(
            ILogger<ModelsController> logger,
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
        public async Task<ActionResult<IEnumerable<ModelDtoV1>>> Get()
        {
            var models = await _db.Models.Find(_ => true).ToListAsync();
            var modelsDtos = models.Select(ModelDtoV1.FromModel);
            var yamlDatas = _yamlSerializer.Serialize(modelsDtos);
            return Content(yamlDatas);
        }

        [HttpGet("{id}")]
        [Produces("text/yaml")]
        public async Task<ActionResult<ModelDtoV1>> GetById(string id)
        {
            var filter = Builders<Model>.Filter.Eq("Id", id);
            var model = await _db.Models.Find(filter).FirstOrDefaultAsync();
            if (model == null)
                return NotFound();

            var modelDto = ModelDtoV1.FromModel(model);
            var yamlData = _yamlSerializer.Serialize(modelDto);
            return Content(yamlData);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<ModelDtoV1>> Apply(ModelDtoV1 modelDto)
        {
            var validationResult = await new ModelDtoV1Validator().ValidateAsync(modelDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            Model model = modelDto.ToModel();

            model.createdAt = DateTime.UtcNow;
            model.createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null;

            #region Delete (if already exists)

            var mFilter = Builders<Models.Model>.Filter.Eq(f => f.name, modelDto.id);
            var existingModel = await _db.Models.Find(mFilter).FirstOrDefaultAsync();

            if (existingModel != null)
            {
                //Delete Pipeline
                await _db.Models.DeleteOneAsync(mFilter);
            }

            #endregion

            await _db.Models.InsertOneAsync(model);

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