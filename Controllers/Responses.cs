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
    public class ResponsesController : ControllerBase
    {
        private readonly ILogger<ResponsesController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        public ResponsesController(
            ILogger<ResponsesController> logger,
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
        public async Task<ActionResult<IEnumerable<ResponseDtoV1>>> Get()
        {
            var responses = await _db.Responses.Find(_ => true).ToListAsync();
            var responsesDtos = responses.Select(ResponseDtoV1.FromResponse);
            var yamlDatas = _yamlSerializer.Serialize(responsesDtos);
            return Content(yamlDatas);
        }

        [HttpGet("{id}")]
        [Produces("text/yaml")]
        public async Task<ActionResult<ResponseDtoV1>> GetById(string id)
        {
            var filter = Builders<Response>.Filter.Eq("Id", id);
            var response = await _db.Responses.Find(filter).FirstOrDefaultAsync();
            if (response == null)
                return NotFound();

            var responseDto = ResponseDtoV1.FromResponse(response);
            var yamlData = _yamlSerializer.Serialize(responseDto);
            return Content(yamlData);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<ResponseDtoV1>> Apply(ResponseDtoV1 responseDto)
        {
            var validationResult = await new ResponseDtoV1Validator().ValidateAsync(responseDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            Response response = responseDto.ToResponse();

            response.createdAt = DateTime.UtcNow;
            response.createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null;

            #region Delete (if already exists)

            var rFilter = Builders<Models.Response>.Filter.Eq(f => f.name, responseDto.id);
            var existingResponse = await _db.Responses.Find(rFilter).FirstOrDefaultAsync();

            if (existingResponse != null)
            {
                //Delete Response
                await _db.Responses.DeleteOneAsync(rFilter);
            }

            #endregion

            await _db.Responses.InsertOneAsync(response);

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
