using Floom.Data;
using Floom.Embeddings;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Floom.Repository;
using Floom.Services;
using Floom.VectorStores;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Floom.Pipeline;

public interface IApplyPipelineUseCase
{
    Task<IActionResult> ExecuteAsync(ApplyPipelineDtoV1 applyPipelineDto);
}

public class ApplyPipelineUseCase : IApplyPipelineUseCase
{
    private readonly ILogger<ApplyPipelineUseCase> _logger;
    private readonly Lazy<IDataService> _dataService;
    private readonly Lazy<IEmbeddingsService> _embeddingsService;
    private readonly Lazy<IModelsService> _modelsService;
    private readonly Lazy<IPromptsService> _promptsService;
    private readonly Lazy<IResponsesService> _responsesService;
    private readonly Lazy<IVectorStoresService> _vectorStoresService;
    private readonly IRepository<OldPipelineEntity> _repository;

    public ApplyPipelineUseCase(ILogger<ApplyPipelineUseCase> logger,
        Lazy<IDataService> dataService,
        Lazy<IEmbeddingsService> embeddingsService,
        Lazy<IModelsService> modelsService,
        Lazy<IPromptsService> promptsService,
        Lazy<IResponsesService> responsesService,
        Lazy<IVectorStoresService> vectorStoresService,
        IRepositoryFactory repositoryFactory)
    {
        _repository = repositoryFactory.Create<OldPipelineEntity>("pipelines");
        _logger = logger;
        _dataService = dataService;
        _embeddingsService = embeddingsService;
        _modelsService = modelsService;
        _promptsService = promptsService;
        _responsesService = responsesService;
        _vectorStoresService = vectorStoresService;
    }


    public async Task<IActionResult> ExecuteAsync(ApplyPipelineDtoV1 applyPipelineDto)
    {
        // check Model auth & name valid
        if (applyPipelineDto.models != null)
        {
            foreach (var model in applyPipelineDto.models)
            {
                _logger.LogInformation("Pipeline Apply - Validating Model " + model.model);

                var modelValid = await _modelsService.Value.Validate(model);

                if (modelValid is not OkObjectResult)
                {
                    return modelValid;
                }
            }
        }

        // check Embeddings auth & name valid
        if (applyPipelineDto.embeddings != null)
        {
            _logger.LogInformation("Pipeline Apply - Validating Embeddings " + applyPipelineDto.embeddings.model);

            var modelValid = await _embeddingsService.Value.Validate(applyPipelineDto.embeddings.ToModel());

            if (modelValid is not OkObjectResult)
            {
                return modelValid;
            }
        }

        // check Store connection
        if (applyPipelineDto.stores != null)
        {
            _logger.LogInformation("Pipeline Apply - Validating Vector Store " + applyPipelineDto.stores.vendor);

            var storeValid = await _vectorStoresService.Value.Validate(applyPipelineDto.stores.ToModel());

            if (storeValid is not OkObjectResult)
            {
                return storeValid;
            }
        }

        // check Assets exists
        if (applyPipelineDto.data != null)
        {
            foreach (var dataDto in applyPipelineDto.data)
            {
                _logger.LogInformation("Pipeline Apply - Validating Data " + dataDto.id);

                var dataValid = await _dataService.Value.Validate(dataDto);

                if (dataValid.Result is not OkObjectResult)
                {
                    return dataValid.Result;
                }
            }
        }

        try
        {
            // upsert embeddings
            if (applyPipelineDto.embeddings != null)
            {
                _logger.LogInformation("Pipeline Apply - Embeddings Apply");

                await _embeddingsService.Value.Apply(applyPipelineDto.embeddings);
            }

            // upsert stores
            if (applyPipelineDto.stores != null)
            {
                _logger.LogInformation("Pipeline Apply - Vector Stores Apply");

                await _vectorStoresService.Value.Apply(applyPipelineDto.stores.ToModel());
            }

            if (applyPipelineDto.data != null)
            {
                _logger.LogInformation("Pipeline Apply - Data PrepareApply");

                var result = await _dataService.Value.PrepareApply(applyPipelineDto);

                if (result is BadRequestObjectResult badRequestResult)
                {
                    throw new Exception(badRequestResult.Value?.ToString());
                }
            }

            // upsert models
            if (applyPipelineDto.models != null)
            {
                _logger.LogInformation("Pipeline Apply - Models Apply");

                await _modelsService.Value.Apply(applyPipelineDto.models.ToArray());
            }

            // upsert prompts
            if (applyPipelineDto.prompt != null)
            {
                _logger.LogInformation("Pipeline Apply - Prompt Apply");

                await _promptsService.Value.Apply(applyPipelineDto.prompt);
            }


            // upsert responses
            if (applyPipelineDto.responses != null)
            {
                _logger.LogInformation("Pipeline Apply - Response Apply");

                await _responsesService.Value.Apply(applyPipelineDto.responses);
            }

            var pipelineEntity = await _repository.Get(applyPipelineDto.id, "name") ??
                                 new OldPipelineEntity { name = applyPipelineDto.id };

            pipelineEntity.schema = applyPipelineDto.schema;
            pipelineEntity.models = applyPipelineDto.models?.Select(x => x.id).ToList();
            pipelineEntity.prompt = applyPipelineDto.prompt?.id;
            pipelineEntity.response = applyPipelineDto.responses?.id;
            pipelineEntity.data = applyPipelineDto.data?.Select(x => x.id).ToList();

            if (pipelineEntity.Id == null || pipelineEntity.Id == ObjectId.Empty)
            {
                _logger.LogInformation("Pipeline Apply - Insert New Pipeline");
                await _repository.Insert(pipelineEntity);
            }
            else
            {
                _logger.LogInformation("Pipeline Apply - Updating Existing Pipeline");
                await _repository.Update(pipelineEntity);
            }
        }
        catch (Exception e)
        {
            return new BadRequestObjectResult(new { Message = $"Error In Apply Pipeline {applyPipelineDto.id}" });
        }

        return new OkObjectResult(new { Message = $"Pipeline applied {applyPipelineDto.id}" });
    }
}