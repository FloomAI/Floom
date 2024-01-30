using Floom.Helpers;
using Floom.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class FilesController : ControllerBase
    {
        private readonly ILogger<FilesController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        private const string FilesDirectory = "/floom/files"; //(env var) [SmallDev: in docker/mounted drive in pc, BigDev: mounted drive in cloud]

        public FilesController(
            ILogger<FilesController> logger,
            IDatabaseService databaseService,
            IDynamicHelpersService dynamicHelpersService,
            IDeserializer yamlDeserializer,
            ISerializer yamlSerializer
        )
        {
            _db = databaseService;
            _dynamicHelpers = new DynamicHelpersService(_db);
        }

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            //# Store File in Storage (env var) [SmallDev: in docker/mounted drive in pc, BigDev: mounted drive in cloud]


            if (Request.Form.Files.Count == 0)
            {
                return BadRequest("No file was provided.");
            }

            var file = Request.Form.Files.FirstOrDefault(); // Get the uploaded file from the request

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was provided.");
            }

            try
            {
                var fileId = Guid.NewGuid();
                var fileExtension = Path.GetExtension(file.FileName);

                // Create the example file directory if it doesn't exist
                if (!Directory.Exists(FilesDirectory))
                {
                    Directory.CreateDirectory(FilesDirectory);
                }

                var storedFile = $"{fileId}{fileExtension}";
                var filePath = Path.Combine(FilesDirectory, $"{storedFile}");

                // Save the uploaded file to the example file directory with a random filename
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Create a new record for MongoDB
                var fileDocument = new Models.File
                {
                    //Id = fileId.ToString(),
                    createdAt = DateTime.UtcNow,
                    createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null,
                    fileId = fileId.ToString(),
                    originalName = file.FileName,
                    storedName = storedFile,
                    storedPath = filePath,
                    extension = fileExtension.ToLower(),
                    size = file.Length // Set the file size in bytes
                };

                // Insert the file record into MongoDB
                await _db.Files.InsertOneAsync(fileDocument);

                // Return the file ID as a response for further reference (optional)
                return Ok(new { FileId = fileId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during file upload.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during file upload.");
            }
        }
    }
}