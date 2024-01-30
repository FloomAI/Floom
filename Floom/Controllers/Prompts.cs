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
    public class PromptsController : ControllerBase
    {
        private readonly ILogger<PromptsController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        public PromptsController(
            ILogger<PromptsController> logger,
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
        public async Task<ActionResult<IEnumerable<PromptDtoV1>>> Get()
        {
            var prompts = await _db.Prompts.Find(_ => true).ToListAsync();
            var promptsDtos = prompts.Select(PromptDtoV1.FromPrompt);
            var yamlDatas = _yamlSerializer.Serialize(promptsDtos);
            return Content(yamlDatas);
        }

        [HttpGet("{id}")]
        [Produces("text/yaml")]
        public async Task<ActionResult<PromptDtoV1>> GetById(string id)
        {
            var filter = Builders<Prompt>.Filter.Eq("Id", id);
            var prompt = await _db.Prompts.Find(filter).FirstOrDefaultAsync();
            if (prompt == null)
                return NotFound();

            var promptDto = PromptDtoV1.FromPrompt(prompt);
            var yamlData = _yamlSerializer.Serialize(promptDto);
            return Content(yamlData);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<PromptDtoV1>> Apply(PromptDtoV1 promptDto)
        {
            var validationResult = await new PromptDtoV1Validator().ValidateAsync(promptDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            Prompt prompt = promptDto.ToPrompt();

            prompt.createdAt = DateTime.UtcNow;
            prompt.createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null;

            #region Delete (if already exists)

            var pFilter = Builders<Models.Prompt>.Filter.Eq(f => f.name, promptDto.id);
            var existingPrompt = await _db.Prompts.Find(pFilter).FirstOrDefaultAsync();

            if (existingPrompt != null)
            {
                //Delete Prompt
                await _db.Prompts.DeleteOneAsync(pFilter);
            }

            #endregion

            await _db.Prompts.InsertOneAsync(prompt);

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
