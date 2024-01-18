using Floom.Managers.AIProviders.Engines;
using Floom.Models;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SharpCompress.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using Floom.Managers.VectorStores.Engines;
using Pinecone;
using Azure.AI.OpenAI;
using System.Security.Policy;
using Floom.Helpers;
using Floom.Managers.AIProviders.Engines.OpenAI2;
using Floom.Managers.VectorStores;
using UglyToad.PdfPig.Graphics.Operations.TextPositioning;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class DataController : ControllerBase
    {
        private readonly ILogger<DataController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        public DataController(
            ILogger<DataController> logger,
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
        public async Task<ActionResult<IEnumerable<DataDtoV1>>> Get()
        {
            var dataSets = await _db.Data.Find(_ => true).ToListAsync();
            var dataSetDtos = dataSets.Select(DataDtoV1.FromData);
            var yamlDataSets = _yamlSerializer.Serialize(dataSetDtos);
            return Content(yamlDataSets);
        }

        [HttpGet("{id}")]
        [Produces("text/yaml")]
        public async Task<ActionResult<DataDtoV1>> GetById(string id)
        {
            var filter = Builders<Data>.Filter.Eq("Id", id);
            var dataSet = await _db.Data.Find(filter).FirstOrDefaultAsync();
            if (dataSet == null)
                return NotFound();

            var dataSetDto = DataDtoV1.FromData(dataSet);
            var yamlDataSet = _yamlSerializer.Serialize(dataSetDto);
            return Content(yamlDataSet);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<DataDtoV1>> Apply(DataDtoV1 dataDto)
        {
            var validationResult = await new DataDtoV1Validator().ValidateAsync(dataDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            //TODO: Check if children actually exist (in validator?)

            Data data = dataDto.ToData();

            List<string> splitText = new List<string>();

            //Type: File
            if (dataDto.type == "file")
            {
                #region Read the file

                //Get the File object
                var filter = Builders<Models.File>.Filter.Eq(f => f.fileId, dataDto.fileId);
                var file = await _db.Files.Find(filter).FirstOrDefaultAsync();

                if (file == null)
                {
                    return BadRequest($"Could not find file {dataDto.fileId}");
                }

                //Check if stored file exists
                if (!System.IO.File.Exists(file.storedPath))
                {
                    return BadRequest($"Could not find file {file.storedPath} on storage");
                }

                byte[] fileBytes;
                try
                {
                    using (var fileStream = new FileStream(file.storedPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await fileStream.CopyToAsync(memoryStream);
                            fileBytes = memoryStream.ToArray();
                        }
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest($"Error while reading file {file.storedPath} on storage");
                }

                #endregion

                #region Parse File (Read content)

                var documentManager = new DocumentManager();

                ExtractionMethod extractionMethod = ExtractionMethod.ByPages;
                switch (dataDto.split)
                {
                    case "pages":
                        extractionMethod = ExtractionMethod.ByPages;
                        break;
                    case "paragraphs":
                        extractionMethod = ExtractionMethod.ByParagraphs;
                        break;
                    case "toc":
                        extractionMethod = ExtractionMethod.ByTOC;
                        break;

                }

                //TODO: Change 500
                splitText = await documentManager.ExtractTextAsync(file.extension, fileBytes, extractionMethod, maxCharactersPerItem: null);

                #endregion
            }

            #region Get EmbeddingsProvider

            var epFilter = Builders<Models.Embeddings>.Filter.Eq(ep => ep.name, dataDto.embeddings);
            var embeddings = await _db.Embeddings.Find(epFilter).FirstOrDefaultAsync();

            if (embeddings == null)
            {
                return BadRequest($"No such EmbeddingsProvider: {dataDto.embeddings}");
            }

            #endregion

            //# TODO: Temporary - Reduce to 3 pages!!!!!!!!!!
            //splitText = splitText;
            //splitText = splitText.GetRange(0, 20);

            #region Send split content to EmbeddingsProvider (OpenAI) (dont worry about conf at first)

            List<List<float>> embeddingsVectors = new List<List<float>>();

            if (embeddings.vendor == "OpenAI")
            {
                OpenAI2 openAiProvider = new OpenAI2(embeddings.apiKey);
                embeddingsVectors = await openAiProvider.GetEmbeddingsAsync(splitText);
            }

            #endregion

            //# Store Embeddings in MDB (Fast switch)

            #region Set VectorStore

            //Data.VectorStore -> ENV VAR -> Internal Milvus

            Models.VectorStore vectorStore = null;

            //If dataDto.VS not provided, use ENV_VAR.VS, if not, use Internal Milvus

            if (dataDto.vectorStore != null && dataDto.vectorStore != string.Empty)
            {
                var vsFilter = Builders<Models.VectorStore>.Filter.Eq(vs => vs.name, dataDto.vectorStore);
                vectorStore = await _db.VectorStores.Find(vsFilter).FirstOrDefaultAsync();

                if (vectorStore == null)
                {
                    //TODO: Try to fetch default ENV_VAR vector data
                    //TODO: PipelineFull don't fail if no Vector Store supplied and ENV_VAR exists

                    //If still both empty, error
                    return BadRequest($"No such VectorStore: {dataDto.vectorStore}");
                }
            }

            //Try ENV_VAR VectorStore
            if (vectorStore == null)
            {
                vectorStore = Helpers.VectorStore.GetEnvVarVectorStore();
            }

            //Try Internal Milvus
            if (vectorStore == null)
            {
                vectorStore = new Models.VectorStore()
                {
                    vendor = "Milvus",
                    endpoint = "standalone",
                    port = 19530
                };
            }

            #endregion

            #region Store Embeddings in VectorStore (Cosine Similarity)

            //Create Index (free: Starter pod, auto/manual pod selection) (Or use existing)
            //Create Collection
            //Insert Vectors (Pinecone with columns in each vector, Milvus columns in collection)

            if (vectorStore.vendor == "Pinecone")
            {
                var pinecone = new Floom.Managers.VectorStores.Engines.Pinecone(
                    vectorStore.apiKey,
                    vectorStore.environment
                );

                //Index name must consist of lower case alphanumeric characters or '-', and must start and end with an alphanumeric character
                string indexName = $"fp-{dataDto.id.ToLower().Replace("_", "-")}"; //TODO: regex this shit in validator of dataset name

                //Check if index exists
                if (await pinecone.IsExistsIndex(indexName))
                {
                    //Delete all vectors in it (Reset it)
                    await pinecone.DeleteAllVectors(indexName);
                }
                else //Not exists, create it 
                {
                    await pinecone.CreateIndex(indexName, 1536, Metric.Cosine);
                }

                int batchSize = 20;

                //Create vectors
                List<Vector> vectors = new List<Vector>();

                for (int page = 0; page < embeddingsVectors.Count; page++)
                {
                    vectors.Add(new Vector()
                    {
                        Id = $"page{page}",
                        Values = embeddingsVectors[page].ToArray(),
                        Metadata = new MetadataMap()
                        {
                            ["text"] = splitText[page],
                            ["page"] = page
                        },
                    });
                }

                //Push Vectors (By Batches)
                for (int i = 0; i < vectors.Count; i += batchSize)
                {
                    // Get the current batch
                    var batch = vectors.Skip(i).Take(batchSize).ToList();

                    // Push Vectors
                    await pinecone.InsertVectors(indexName, batch);
                }
            }

            if (vectorStore.vendor == "Milvus")
            {
                var milvus = new Floom.Managers.VectorStores.Engines.Milvus(
                    Endpoint: vectorStore.endpoint,
                    Port: (UInt16)vectorStore.port,
                    Username: vectorStore.username,
                    Password: vectorStore.password
                );

                //Index name must consist of lower case alphanumeric characters or '-', and must start and end with an alphanumeric character
                string collectionName = $"fp_{dataDto.id.ToLower().Replace("-", "_")}"; //TODO: regex this shit in validator of dataset name

                //Check if index exists
                if (await milvus.IsExistsCollection(collectionName))
                {
                    //Delete entire collection
                    await milvus.DeleteCollection(collectionName);
                }

                //Milvus DeleteVectors is quite slow therefore we delete the entire collection and regenerate
                await milvus.CreateCollection(collectionName, 1536);
                await milvus.CreateIndex(collectionName);
                await milvus.LoadToMemory(collectionName);

                int batchSize = 20;

                //Create vectors
                List<SectionVectors> vectors = new List<SectionVectors>();

                for (int page = 0; page < embeddingsVectors.Count; page++)
                {
                    vectors.Add(new SectionVectors()
                    {
                        id = $"page{page}",
                        values = embeddingsVectors[page],
                        text = splitText[page] //TODO: SHOULD TRIM IN Milvus to max varchar length
                    });
                }

                //Push Vectors (By Batches)
                for (int i = 0; i < vectors.Count; i += batchSize)
                {
                    // Get the current batch
                    var batch = vectors.Skip(i).Take(batchSize).ToList();

                    // Push Vectors
                    await milvus.InsertVectors(collectionName, batch);
                }
            }

            #endregion

            //# Store DataSet

            //Add metadata to DataSet
            data.createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null;
            data.createdAt = DateTime.UtcNow;

            #region Delete (if already exists)

            var dFilter = Builders<Models.Data>.Filter.Eq(f => f.name, dataDto.id);
            var existingData = await _db.Data.Find(dFilter).FirstOrDefaultAsync();

            if (existingData != null)
            {
                //Delete Data
                await _db.Data.DeleteOneAsync(dFilter);
            }

            #endregion

            await _db.Data.InsertOneAsync(data);

            await _dynamicHelpers.AuditAsync(
                action: AuditAction.Create,
                objectType: "data",
                objectId: data.Id.ToString(),
                objectName: data.name,
                httpContext: HttpContext
            );

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var filter = Builders<Data>.Filter.Eq("Id", id);
            var result = await _db.Data.DeleteOneAsync(filter);
            if (result.DeletedCount == 0)
                return NotFound();

            return Ok();
        }
    }
}
