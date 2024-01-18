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
using Microsoft.AspNetCore.Http.HttpResults;
using static iText.StyledXmlParser.Jsoup.Select.Evaluator;
using System.Security.Cryptography.X509Certificates;
using Floom.Models.Other.Floom;
using Microsoft.Extensions.Azure;
using Floom.Managers.VectorStores;
using Floom.Managers.AIProviders;
using Floom.Misc;
using Floom.Models.Other.Floom;
using Microsoft.AspNetCore.Http.Extensions;
using Floom.Helpers;
using Floom.Managers.AIProviders.Engines.OpenAI2;
using PdfSharp.Drawing;

namespace Floom.Controllers
{
    [ApiController]
    [Route("/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [ApiKeyAuthorization]
    public class PipelinesController : ControllerBase
    {
        private readonly ILogger<PipelinesController> _logger;
        private readonly IDatabaseService _db;
        private readonly IDynamicHelpersService _dynamicHelpers;
        private readonly IDeserializer _yamlDeserializer;
        private readonly ISerializer _yamlSerializer;

        public PipelinesController(
            ILogger<PipelinesController> logger,
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
        public async Task<ActionResult<IEnumerable<PipelineDtoV1>>> Get()
        {
            var pipelines = await _db.Pipelines.Find(_ => true).ToListAsync();
            var pipelineDtos = pipelines.Select(PipelineDtoV1.FromPipeline);
            var yamlPipelines = _yamlSerializer.Serialize(pipelineDtos);
            return Content(yamlPipelines);
        }

        [HttpGet("{id}")]
        [Produces("text/yaml")]
        public async Task<ActionResult<PipelineDtoV1>> GetById(string id)
        {
            var filter = Builders<Pipeline>.Filter.Eq("Id", id);
            var pipeline = await _db.Pipelines.Find(filter).FirstOrDefaultAsync();
            if (pipeline == null)
                return NotFound();

            var pipelineDto = PipelineDtoV1.FromPipeline(pipeline);
            var yamlPipeline = _yamlSerializer.Serialize(pipelineDto);

            return Content(yamlPipeline);
        }

        [HttpPost("Apply")]
        public async Task<ActionResult<PipelineDtoV1>> Apply(PipelineDtoV1 pipelineDto)
        {
            var validationResult = await new PipelineDtoV1Validator().ValidateAsync(pipelineDto);
            if (!validationResult.IsValid)
                return BadRequest(validationResult.Errors);

            Pipeline pipeline = pipelineDto.ToPipeline();

            //Make sure that all objects exist
            //Build PipelineFull
            await BuildPipelineFullAsync(pipeline);

            //Add metadata to Pipeline
            pipeline.createdBy = (HttpContext.Items.TryGetValue("API_KEY_DETAILS", out var apiKeyDetailsObj) && apiKeyDetailsObj is ApiKey apiKeyDocument) ? apiKeyDocument.Id : null;
            pipeline.createdAt = DateTime.UtcNow;

            #region Delete (if already exists)

            var plFilter = Builders<Models.Pipeline>.Filter.Eq(f => f.name, pipelineDto.id);
            var existingPipeline = await _db.Pipelines.Find(plFilter).FirstOrDefaultAsync();

            if (existingPipeline != null)
            {
                //Delete Pipeline
                await _db.Pipelines.DeleteOneAsync(plFilter);
            }

            #endregion

            await _db.Pipelines.InsertOneAsync(pipeline);

            await _dynamicHelpers.AuditAsync(
                   action: AuditAction.Create,
                   objectType: "pipeline",
                   objectId: pipeline.Id.ToString(),
                   objectName: pipeline.name,
                   httpContext: HttpContext
            );

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var filter = Builders<Pipeline>.Filter.Eq("Id", id);
            var result = await _db.Pipelines.DeleteOneAsync(filter);
            if (result.DeletedCount == 0)
                return NotFound();

            await _dynamicHelpers.AuditAsync(
                   action: AuditAction.Delete,
                   objectType: "pipeline",
                   objectId: id,
                   httpContext: HttpContext
            );

            return Ok();
        }

        //JSON
        //Think of chat, not only message. Keep Session history somewhere based on "Session ID".
        //Think of multimodel (text, image, audio, video INPUT/OUTPUT)
        //Think of static prompt / dynamic one + variables (SDK/API)[
        [HttpPost("Run")]
        [Consumes("application/json")]
        public async Task<IActionResult> Run(
            FloomRequest floomRequest
            )
        {
            #region Set MessageID + ChatID Guids

            string messageId = Guid.NewGuid().ToString();
            string chatId = floomRequest.chatId != string.Empty ? floomRequest.chatId : Guid.NewGuid().ToString();

            #endregion

            #region Get Pipeline (Full)

            PipelineFull pipelineFull = new PipelineFull();
            try
            {
                pipelineFull = await GetPipelineFullAsync(floomRequest.pipelineId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            #endregion

            #region Compile Prompt

            PromptRequest promptRequest = new PromptRequest();

            #region Add Chat Previous Messages (History)

            //Fetch all Audits with relevant ChatID (if provided by FloomRequest)

            if (floomRequest.chatId != string.Empty)
            {
                var filterChatAudits = Builders<AuditRow>.Filter.Eq("chatId", floomRequest.chatId);
                var auditRecords = (await _db.Audit.Find(filterChatAudits).ToListAsync()).OrderBy(ar => ar.createdAt);

                //Add system for first record only
                if (auditRecords.Count() != 0)
                {
                    if (auditRecords.First().attributes["compiledPromptRequestSystem"] != null)
                    {
                        promptRequest.previousMessages.Add(new PromptMessage()
                        {
                            role = "system",
                            content = auditRecords.First().attributes["compiledPromptRequestSystem"].ToString()
                        });
                    }
                }

                //Add system? only once
                foreach (AuditRow auditRecord in auditRecords)
                {
                    promptRequest.previousMessages.Add(new PromptMessage()
                    {
                        role = "user", //user input
                        content = auditRecord.attributes["compiledPromptRequestUser"].ToString()
                    });

                    promptRequest.previousMessages.Add(new PromptMessage()
                    {
                        role = "assistant", //answer
                        content = auditRecord.attributes["promptResponse"].ToString()
                    });
                }
            }

            //Iterate and add (System+User+Assitant)

            #endregion

            //If system exists, add
            if (pipelineFull.prompt.system != null)
            {
                promptRequest.system = pipelineFull.prompt.system.CompileWithVariables(floomRequest.variables);
            }

            //If Pipeline.Prompt.User exists
            if (pipelineFull.prompt.user != null && pipelineFull.prompt.user != string.Empty)
            {
                promptRequest.user = pipelineFull.prompt.user.CompileWithVariables(floomRequest.variables);
            }
            else //No Pipeline.Prompt.User
            {
                //Grab from FloomRequest
                promptRequest.user = floomRequest.input.CompileWithVariables(floomRequest.variables);
            }

            #endregion

            #region Handle Data

            if (pipelineFull.data != null && pipelineFull.data.Count > 0)
            {
                //List of Similary Search Results (from all Datas)
                List<VectorSearchResult> vectorSearchResults = new List<VectorSearchResult>();

                //Iterate Data
                foreach (DataFull data in pipelineFull.data)
                {
                    #region Get Query Embeddings

                    List<List<float>> queryEmbeddings = new List<List<float>>();

                    if (data.embeddings != null) //Use specific Embeddings engine
                    {
                        if (data.embeddings.vendor == "OpenAI")
                        {
                            OpenAI2 openAiProvider = new OpenAI2(data.embeddings.apiKey);
                            queryEmbeddings = await openAiProvider.GetEmbeddingsAsync(
                                new List<string>() {
                                    promptRequest.user
                                }
                            );
                        }
                    }
                    else //Use chosen Model for embeddings
                    {
                        if (pipelineFull.model.vendor == "OpenAI")
                        {
                            OpenAI2 openAiProvider = new OpenAI2(pipelineFull.model.apiKey);
                            queryEmbeddings = await openAiProvider.GetEmbeddingsAsync(
                             new List<string>() {
                                promptRequest.user
                             }
                            );
                        }
                    }

                    #endregion

                    #region Similarity Search

                    //Data.VectorStore -> ENV VAR -> Internal Milvus

                    Models.VectorStore vectorStore = null;

                    if (data.vectorStore != null && data.vectorStore.vendor != string.Empty)
                    {
                        vectorStore = data.vectorStore;
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

                    if (vectorStore.vendor == "Pinecone")
                    {
                        var pinecone = new Floom.Managers.VectorStores.Engines.Pinecone(
                            vectorStore.apiKey,
                            vectorStore.environment
                        );

                        //Index name must consist of lower case alphanumeric characters or '-', and must start and end with an alphanumeric character
                        string indexName = $"fp-{data.name.ToLower().Replace("_", "-")}"; //TODO: regex this shit in validator of dataset name

                        //Check if index exists
                        if (!await pinecone.IsExistsIndex(indexName))
                        {
                            return BadRequest($"VectorStore index is faulty. Please regenerate Data.");
                        }

                        //Get query results
                        List<VectorSearchResult> results = await pinecone.Search(
                            indexName,
                            queryEmbeddings.First(),
                            metadataFilter: null,
                            topResults: 3
                            );

                        //Load to overall search 
                        vectorSearchResults.AddRange(results);
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
                        string collectionName = $"fp_{data.name.ToLower().Replace("-", "_")}"; //TODO: regex this shit in validator of dataset name

                        //Check if index exists
                        if (!await milvus.IsExistsCollection(collectionName))
                        {
                            return BadRequest($"VectorStore collection is faulty. Please regenerate Data.");
                        }

                        //Get query results
                        List<VectorSearchResult> results = await milvus.Search(
                            collectionName,
                            queryEmbeddings.First(),
                            //metadataFilter: null,
                            topResults: 3
                            );

                        //Load to overall search 
                        vectorSearchResults.AddRange(results);
                    }

                    #endregion
                }

                //Check if search has results
                if (vectorSearchResults.Count == 0)
                {
                    return BadRequest("Search query didn't yield any results.");
                }

                //Merge several results text
                string mergedResults = string.Empty;
                foreach (var vectorSearchResult in vectorSearchResults.Take(3))
                {
                    mergedResults += $"{vectorSearchResult.text}. \n";
                }

                //Add Data to the System's Prompt (Make sure it ends with dot.)
                promptRequest.system += $"Answer directly and shortly, no additions, just the answer itself. "; //Direct answer

                //Refer to Data
                //if (!pipelineFull.response.referToData)
                //{
                //    promptRequest.system += $"\n Don't refer to the pages, sections or chapters in documentation. ";
                //}

                //Max Sentences
                if (pipelineFull.response.maxSentences != 0)
                {
                    promptRequest.system += $" Don't answer in more than {pipelineFull.response.maxSentences} sentences.";
                }

                //Max Charcters
                if (pipelineFull.response.maxCharacters != 0)
                {
                    promptRequest.system += $" Don't answer in more than {pipelineFull.response.maxCharacters} characters in total.";
                }

                //Language
                if (pipelineFull.response.language != null)
                {
                    promptRequest.system += $" Please answer in the {pipelineFull.response.language} language.";
                }

                //Examples (Few Shots)
                if (pipelineFull.response != null && pipelineFull.response.examples != null && pipelineFull.response.examples.Count > 0)
                {
                    promptRequest.system += $"  Example of answers: ";
                    foreach (var example in pipelineFull.response.examples)
                    {
                        promptRequest.system += $" \"{example}\", ";
                    }
                }

                promptRequest.system += $" \n You answer questions based on provided documentation section."; //Answer per documentation
                promptRequest.system += $" \n The documentation section is: '{mergedResults}' "; //Documentation supplied

                #region Query AI 

                if (pipelineFull.model.vendor == "OpenAI")
                {
                    OpenAI2 openAI = new OpenAI2(pipelineFull.model.apiKey);

                    PromptResponse promptResponse = await openAI.GenerateTextAsync(
                            prompt: promptRequest,
                            model: pipelineFull.model.model
                    );

                    var floomResponse = new FloomResponse()
                    {
                        messageId = messageId,
                        chatId = chatId,
                        values = promptResponse.values,
                        processingTime = promptResponse.elapsedProcessingTime,
                        tokenUsage = promptResponse.tokenUsage == null ? new FloomResponseTokenUsage() : promptResponse.tokenUsage.ToFloomResponseTokenUsage()
                    };

                    await _dynamicHelpers.AuditAsync(
                        action: AuditAction.Floom,
                        objectType: "pipeline",
                        objectId: pipelineFull.Id.ToString(),
                        objectName: pipelineFull.name,
                        messageId: messageId,
                        chatId: chatId,
                        attributes: new Dictionary<string, object>()
                        {
                            { "request", floomRequest.input },
                            { "requestVariables", floomRequest.variables },
                            { "compiledPromptRequestUser", promptRequest.user },
                            { "compiledPromptRequestSystem", promptRequest.system },
                            { "promptResponse", promptResponse.values.First().value.ToString() },
                            { "promptResponseProrcessingTokens", promptResponse.tokenUsage.processingTokens },
                            { "promptResponsePromptTokens", promptResponse.tokenUsage.promptTokens },
                            { "promptResponseTotalTokens", promptResponse.tokenUsage.totalTokens },
                            { "promptResponseProcessingTime", promptResponse.elapsedProcessingTime }
                        },
                        httpContext: HttpContext
                    );

                    return Ok(floomResponse);
                }

                #endregion
            }

            #endregion

            //Query (No Data)
            if (pipelineFull.model.vendor == "OpenAI")
            {
                OpenAI2 openAI = new OpenAI2(pipelineFull.model.apiKey);

                //Create general prompt response
                PromptResponse promptResponse = new PromptResponse();

                //If Text generation
                if (pipelineFull.response.type == ResponseType.Text)
                {
                    promptResponse = await openAI.GenerateTextAsync(
                        prompt: promptRequest,
                        model: pipelineFull.model.model
                    );

                    await _dynamicHelpers.AuditAsync(
                        action: AuditAction.Floom,
                        objectType: "pipeline",
                        objectId: pipelineFull.Id.ToString(),
                        objectName: pipelineFull.name,
                        messageId: messageId,
                        chatId: chatId,
                        attributes: new Dictionary<string, object>()
                        {
                            { "request", floomRequest.input },
                            { "requestVariables", floomRequest.variables },
                            { "compiledPromptRequestUser", promptRequest.user },
                            { "compiledPromptRequestSystem", promptRequest.system },
                            { "promptResponse", promptResponse.values.First().value.ToString() },
                            { "promptResponseProrcessingTokens", promptResponse.tokenUsage.processingTokens },
                            { "promptResponsePromptTokens", promptResponse.tokenUsage.promptTokens },
                            { "promptResponseTotalTokens", promptResponse.tokenUsage.totalTokens },
                            { "promptResponseProcessingTime", promptResponse.elapsedProcessingTime }
                        },
                        httpContext: HttpContext
                    );
                }

                //If Image Generation
                if (pipelineFull.response.type == ResponseType.Image)
                {
                    //Add Resolution
                    if (pipelineFull.response.resolution != null && pipelineFull.response.resolution != string.Empty)
                    {
                        promptRequest.resolution = pipelineFull.response.resolution;
                    }

                    //Add Number of Options
                    promptRequest.options = pipelineFull.response.options;

                    promptResponse = await openAI.GenerateImageAsync(
                       prompt: promptRequest,
                       model: pipelineFull.model.model
                    );

                    await _dynamicHelpers.AuditAsync(
                        action: AuditAction.Floom,
                        objectType: "pipeline",
                        objectId: pipelineFull.Id.ToString(),
                        objectName: pipelineFull.name,
                        messageId: messageId,
                        chatId: chatId,
                        attributes: new Dictionary<string, object>()
                        {
                            { "request", floomRequest.input },
                            { "requestVariables", floomRequest.variables },
                            { "compiledPromptRequestUser", promptRequest.user },
                            { "promptResponse", promptResponse.values.First().value.ToString() },
                            //{ "promptResponseProrcessingTokens", promptResponse.tokenUsage.processingTokens },
                            //{ "promptResponsePromptTokens", promptResponse.tokenUsage.promptTokens },
                            //{ "promptResponseTotalTokens", promptResponse.tokenUsage.totalTokens },
                            { "promptResponseProcessingTime", promptResponse.elapsedProcessingTime }
                        },
                        httpContext: HttpContext
                    );
                }

                //Convert PromptResponse (internal) to FloomResponse (external)
                var floomResponse = new FloomResponse()
                {
                    messageId = messageId,
                    chatId = chatId,
                    values = promptResponse.values,
                    processingTime = promptResponse.elapsedProcessingTime,
                    tokenUsage = promptResponse.tokenUsage == null ? new FloomResponseTokenUsage() : promptResponse.tokenUsage.ToFloomResponseTokenUsage()
                };

                return Ok(floomResponse);

                //if (prompt.)

                //Get PipelineID + Input (object) + Variables (text most likely)
                //When text IN, variables are likely
                //When multimedia IN, variables are unlikely - but settings.

                //If dataset, first use embedding for the query. query the vdb for cosine similarity.
                //If several datasets, query all, order by score.
                //Take first answer and get metadata.text from it. Send to LLM scoped somehow.

                //If variables, inject, also go to API to grab variables (VariablesProvider)

                //Also OutputSettings: max length, max sentences, language, mood(personality) 

                //Allow Security + Good Language

                //Allow "Function Calling"

                //Define also OutputValidation, "Code" (as input/output),
                //allow simple code syntax validation, 
                //allow code malware scanning (send to LLM again ask if malicious)

            }

            return BadRequest();
        }

        private async Task<PipelineFull> GetPipelineFullAsync(string pipelineId)
        {
            Pipeline pipeline = new Pipeline();

            #region Get Pipeline

            var filterPipeline = Builders<Pipeline>.Filter.Eq("name", pipelineId);
            pipeline = await _db.Pipelines.Find(filterPipeline).FirstOrDefaultAsync();
            if (pipeline == null)
                throw new Exception($"Pipeline '{pipelineId}' not found.");

            #endregion

            return await BuildPipelineFullAsync(pipeline);
        }

        private async Task<PipelineFull> BuildPipelineFullAsync(Pipeline pipeline)
        {
            PipelineFull pipelineFull = new PipelineFull();

            #region Get Pipeline

            //var filterPipeline = Builders<Pipeline>.Filter.Eq("name", pipeline.name);
            //pipeline = await _db.Pipelines.Find(filterPipeline).FirstOrDefaultAsync();
            //if (pipeline == null)
            //    throw new Exception($"Pipeline '{pipeline.name}' not found.");

            #endregion

            //Convert to PipelineFull
            pipelineFull = new PipelineFull()
            {
                Id = pipeline.Id,
                name = pipeline.name,
                createdAt = pipeline.createdAt,
                createdBy = pipeline.createdBy,
                chatHistory = pipeline.chatHistory,
                data = new List<DataFull>()
            };

            #region Get Model

            if (pipeline.model != string.Empty)
            {
                var filterModel = Builders<Model>.Filter.Eq("name", pipeline.model);
                var model = await _db.Models.Find(filterModel).FirstOrDefaultAsync();
                if (model == null)
                    throw new Exception($"Pipeline's Model '{pipeline.model}' not found.");

                pipelineFull.model = model;
            }

            #endregion


            #region Get Prompt

            if (pipeline.prompt != string.Empty)
            {
                var filterPrompt = Builders<Prompt>.Filter.Eq("name", pipeline.prompt);
                var prompt = await _db.Prompts.Find(filterPrompt).FirstOrDefaultAsync();
                if (prompt == null)
                    throw new Exception($"Pipeline's Prompt '{pipeline.prompt}' not found.");

                pipelineFull.prompt = prompt;
            }

            #endregion

            #region Get Response

            if (pipeline.prompt != string.Empty)
            {
                var filterResponse = Builders<Response>.Filter.Eq("name", pipeline.response);
                var response = await _db.Responses.Find(filterResponse).FirstOrDefaultAsync();
                if (response == null)
                    throw new Exception($"Pipeline's Response '{pipeline.response}' not found.");

                pipelineFull.response = response;
            }

            #endregion

            #region Get Data

            if (pipeline.data != null && pipeline.data.Count != 0)
            {
                foreach (string dataRef in pipeline.data)
                {
                    var filterData = Builders<Data>.Filter.Eq("name", dataRef);
                    var data = await _db.Data.Find(filterData).FirstOrDefaultAsync();
                    if (data == null)
                        throw new Exception($"Pipeline's Data '{data}' not found.");

                    //Convert to DataFull
                    DataFull dataFull = new DataFull()
                    {
                        Id = data.Id,
                        name = data.name,
                        createdAt = data.createdAt,
                        createdBy = data.createdBy,
                        split = data.split,
                        type = data.type
                    };

                    #region Get Embeddings

                    if (data.embeddings != null && data.embeddings != string.Empty)
                    {
                        var filterEmbeddings = Builders<Models.Embeddings>.Filter.Eq("name", data.embeddings);
                        var embeddings = await _db.Embeddings.Find(filterEmbeddings).FirstOrDefaultAsync();
                        if (embeddings == null)
                            throw new Exception($"Pipeline.{data.name}.Embeddings '{data.embeddings}' not found.");

                        dataFull.embeddings = embeddings;
                    }

                    #endregion

                    #region Get VectorStore

                    if (data.vectorStore != null && data.vectorStore != string.Empty)
                    {
                        var filterVS = Builders<Models.VectorStore>.Filter.Eq("name", data.vectorStore);
                        var vectorStore = await _db.VectorStores.Find(filterVS).FirstOrDefaultAsync();
                        if (vectorStore == null)
                            throw new Exception($"Pipeline.{data.vectorStore}.Embeddings '{data.vectorStore}' not found.");

                        dataFull.vectorStore = vectorStore;
                    }

                    #endregion

                    #region Get File

                    if (data.fileId != string.Empty)
                    {
                        var filterFile = Builders<Models.File>.Filter.Eq("fileId", data.fileId);
                        var file = await _db.Files.Find(filterFile).FirstOrDefaultAsync();
                        if (file == null)
                            throw new Exception($"Pipeline.{data}.File '{data.fileId}' not found.");

                        dataFull.file = file;
                    }

                    #endregion

                    pipelineFull.data.Add(dataFull);
                }
            }

            #endregion

            return pipelineFull;
        }
    }
}