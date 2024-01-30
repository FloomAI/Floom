using Floom.Audit;
using Floom.Context.Embeddings;
using Floom.Context.VectorStores;
using Floom.Embeddings;
using Floom.Entities.AuditRow;
using Floom.Entities.Response;
using Floom.LLMs;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Floom.Utils;
using Floom.VectorStores;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace Floom.Pipeline;

public interface IRunPipelineUseCase
{
    Task<IActionResult> ExecuteAsync(FloomRequest floomRequest);
}

public class RunPipelineUseCase : IRunPipelineUseCase
{
    private readonly ILogger<RunPipelineUseCase> _logger;
    private readonly FloomAuditService _auditService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IGetPipelineUseCase _getPipelineUseCase;

    public RunPipelineUseCase(ILogger<RunPipelineUseCase> logger,
        FloomAuditService auditService,
        IServiceProvider serviceProvider,
        IGetPipelineUseCase getPipelineUseCase)
    {
        _logger = logger;
        _auditService = auditService;
        _serviceProvider = serviceProvider;
        _getPipelineUseCase = getPipelineUseCase;
    }

    public async Task<IActionResult> ExecuteAsync(FloomRequest floomRequest)
    {
        _logger.LogInformation("Pipeline Run - Start");

        #region Set MessageID + ChatID Guids

        var messageId = Guid.NewGuid().ToString();
        var chatId = floomRequest.chatId != string.Empty ? floomRequest.chatId : Guid.NewGuid().ToString();

        #endregion

        #region Get Pipeline (Full)

        OldPipelineModel oldPipelineModel;

        _logger.LogInformation("Pipeline Run - Validating");

        var result = await _getPipelineUseCase.ExecuteAsync(floomRequest.pipelineId);

        if (result is OkObjectResult okResult)
        {
            oldPipelineModel = (okResult.Value as OldPipelineModel)!;
        }
        else
        {
            return result;
        }

        #endregion Get Pipeline (Full)

        var promptRequest = new PromptRequest();

        #region Add Chat Previous Messages (History)

        //Fetch all Audits with relevant ChatID (if provided by FloomRequest)

        if (floomRequest.chatId != string.Empty)
        {
            var filterChatAudits = Builders<AuditRowEntity>.Filter.Eq("chatId", floomRequest.chatId);
            var auditRecords = await _auditService.GetByChatId(floomRequest.chatId);
            // sort audit records
            auditRecords = auditRecords.OrderBy(x => x.createdAt);

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
            foreach (var auditRecord in auditRecords)
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

        #endregion Compile Prompt

        //
        //If system exists, add
        if (oldPipelineModel.Prompt != null)
        {
            if (oldPipelineModel.Prompt.system != null)
            {
                promptRequest.system = oldPipelineModel.Prompt.system.CompileWithVariables(floomRequest.variables);
            }

            // If Pipeline.Prompt.User exists
            if (!string.IsNullOrEmpty(oldPipelineModel.Prompt.user))
            {
                promptRequest.user = oldPipelineModel.Prompt.user.CompileWithVariables(floomRequest.variables);
            }
        }

        //Grab from FloomRequest
        if (floomRequest.input != null)
        {
            promptRequest.user = floomRequest.input.CompileWithVariables(floomRequest.variables);
        }

        #region Handle Data

        _logger.LogInformation("Pipeline Run - Handle Data");

        //List of Similary Search Results (from all Datas)
        var vectorSearchResults = new List<VectorSearchResult>();
        var mergedResults = string.Empty;

        if (oldPipelineModel.Data != null && oldPipelineModel.Data.Count > 0)
        {
            //Iterate Data
            foreach (var data in oldPipelineModel.Data)
            {
                #region Get Query Embeddings

                var queryEmbeddings = new List<List<float>>();
                if (data.Embeddings != null) //Use specific Embeddings engine
                {
                    _logger.LogInformation("Get Query Embeddings");

                    // var embeddingsProvider = EmbeddingsFactory.GetFactory(_serviceProvider)
                        // .Create(data.Embeddings.vendor, data.Embeddings.apiKey);

                    // queryEmbeddings = await embeddingsProvider.GetEmbeddingsAsync(new List<string>()
                        // {
                            // promptRequest.user
                        // }
                    // );
                }

                #endregion

                #region Similarity Search

                _logger.LogInformation("Similarity Search");


                if (data.VectorStore != null)
                {
                    // var vectorStoreProvider = VectorStoresFactory.GetFactory(_serviceProvider).Create(data.VectorStore);
                    // vectorStoreProvider.CollectionName =
                    //     VectorStores.Utils.GetCollectionName(data.name, data.Embeddings.model);
                    // List<VectorSearchResult> results = await vectorStoreProvider.Search(
                    //     queryEmbeddings.First(),
                    //     topResults: 3
                    // );

                    // vectorSearchResults.AddRange(results);
                }

                #endregion
            }

            //Check if search has results
            if (vectorSearchResults.Count == 0)
            {
                return new BadRequestObjectResult(new { Message = $"Search query didn't yield any results." });
            }

            //Merge several results text
            foreach (var vectorSearchResult in vectorSearchResults.Take(3))
            {
                mergedResults += $"{vectorSearchResult.text}. \n";
            }

            //Add Data to the System's Prompt (Make sure it ends with dot.)
            promptRequest.system +=
                $"Answer directly and shortly, no additions, just the answer itself. "; //Direct answer


            promptRequest.system +=
                $" \n You answer questions based on provided documentation section."; //Answer per documentation
            promptRequest.system += $" \n The documentation section is: '{mergedResults}' "; //Documentation supplied
        }

        //Refer to Data
        if (oldPipelineModel.Response != null)
        {
            if (!oldPipelineModel.Response.referToData)
            {
                promptRequest.system += $"\n Don't refer to the pages, sections or chapters in documentation. ";
            }

            //Max Sentences
            if (oldPipelineModel.Response.maxSentences != 0)
            {
                promptRequest.system +=
                    $" Don't answer in more than {oldPipelineModel.Response.maxSentences} sentences.";
            }

            //Max Characters
            if (oldPipelineModel.Response.maxCharacters != 0)
            {
                promptRequest.system +=
                    $" Don't answer in more than {oldPipelineModel.Response.maxCharacters} characters in total.";
            }

            //Language
            if (oldPipelineModel.Response.language != null)
            {
                promptRequest.system += $" Please answer in the {oldPipelineModel.Response.language} language.";
            }

            //Examples (Few Shots)

            if (oldPipelineModel.Response.examples is { Count: > 0 })
            {
                _logger.LogInformation("Pipeline Few Shots");

                promptRequest.system += $"  Example of answers: ";
                foreach (var example in oldPipelineModel.Response.examples)
                {
                    promptRequest.system += $" \"{example}\", ";
                }
            }
        }

        #region Query AI

        _logger.LogInformation("Pipeline Query LLM");


        try
        {
            PromptResponse promptResponse;

            var model = oldPipelineModel.Models.First();
            var llmProvider = LLMFactory.GetFactory(_serviceProvider).Create(model.vendor);
            llmProvider.SetApiKey(model.apiKey);

            if (oldPipelineModel.Response == null)
            {
                promptResponse = await llmProvider.GenerateTextAsync(
                    prompt: promptRequest,
                    model: model.model
                );
            }
            else
            {
                switch (oldPipelineModel.Response)
                {
                    case { type: ResponseType.Text }:
                        promptResponse = await llmProvider.GenerateTextAsync(
                            prompt: promptRequest,
                            model: model.model
                        );
                        break;
                    case { type: ResponseType.Image }:
                    {
                        if (llmProvider is not LLMImageProvider)
                        {
                            return new BadRequestObjectResult(new
                                { Message = $"LLM ${model.vendor} does not support image." });
                        }

                        var llmImageProvider = (LLMImageProvider)llmProvider;
                        //Add Resolution
                        if (!string.IsNullOrEmpty(oldPipelineModel.Response.resolution))
                        {
                            promptRequest.resolution = oldPipelineModel.Response.resolution;
                        }

                        //Add Number of Options
                        promptRequest.options = oldPipelineModel.Response.options;

                        promptResponse = await llmImageProvider.GenerateImageAsync(
                            prompt: promptRequest,
                            model: model.model
                        );
                        break;
                    }
                    default:
                        return new BadRequestObjectResult(new { Message = $"Response type not supported." });
                }
            }


            var floomResponse = new FloomResponse()
            {
                messageId = messageId,
                chatId = chatId,
                values = promptResponse.values,
                processingTime = promptResponse.elapsedProcessingTime,
                tokenUsage = promptResponse.tokenUsage == null
                    ? new FloomResponseTokenUsage()
                    : promptResponse.tokenUsage.ToFloomResponseTokenUsage()
            };

            _logger.LogInformation("Pipeline Got Response");

            _auditService.Insert(
                action: AuditAction.Floom,
                objectType: "pipeline",
                objectId: oldPipelineModel.Id.ToString(),
                objectName: oldPipelineModel.name,
                messageId: messageId,
                chatId: chatId,
                attributes: FloomAuditUtils.GetPipelineAttributes(floomRequest, promptRequest, promptResponse)
            );

            return new OkObjectResult(floomResponse);
        }
        catch (Exception err)
        {
            Console.Write(err);
        }

        #endregion

        #endregion

        return new BadRequestObjectResult("Not Available");
    }
}