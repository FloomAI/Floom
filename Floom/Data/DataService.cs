using Floom.Base;
using Floom.Data.Entities;
using Floom.Embeddings;
using Floom.Pipeline;
using Floom.Pipeline.Entities.Dtos;
using Floom.Repository;
using Floom.Services;
using Floom.VectorStores;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Data;

public interface IDataService
{
    Task<IEnumerable<DataDto>> GetAll();
    Task<DataDto?> GetById(string id);
    Task<DataModel?> GetDataById(string id);
    Task<IActionResult> PrepareApply(ApplyPipelineDtoV1 applyPipelineDtoV1);
    Task<IActionResult> PrepareApply(DataDto dataDto);
    Task<ActionResult<int>> Validate(DataDto modelDto);
}

public class DataService : ServiceBase, IDataService
{
    private readonly IRepository<DataEntity> _repository;
    private readonly Lazy<IPipelinesService> _pipelineService;
    private readonly Lazy<IEmbeddingsService> _embeddingsService;
    private readonly Lazy<IVectorStoresService> _vectorStoresService;
    private readonly Lazy<IDataApplyUseCase> _dataApplyUseCase;

    public DataService(
        IRepositoryFactory repositoryFactory,
        Lazy<IPipelinesService> pipelineService,
        Lazy<IEmbeddingsService> embeddingsService,
        Lazy<IVectorStoresService> vectorStoresService,
        Lazy<IDataApplyUseCase> dataApplyUseCase,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _repository = repositoryFactory.Create<DataEntity>("data");
        _pipelineService = pipelineService;
        _embeddingsService = embeddingsService;
        _vectorStoresService = vectorStoresService;
        _dataApplyUseCase = dataApplyUseCase;
    }

    public async Task<IEnumerable<DataDto>> GetAll()
    {
        _logger.LogInformation("GetAlL");
        var models = await _repository.GetAll();
        var dtos = models.Select(DataDto.FromData);
        return dtos;
    }

    public async Task<DataDto?> GetById(string id)
    {
        var model = await _repository.Get(id, "name");
        return DataDto.FromData(model);
    }

    public async Task<DataModel?> GetDataById(string id)
    {
        var model = await _repository.Get(id, "name");
        if (model == null) return null;
        var data = new DataModel
        {
            type = model.type,
            // Asset = await _assetsService.Value.GetModelByAssetId(model.assetId),
            split = model.split,
            Embeddings = await _embeddingsService.Value.GetById(model.embeddings),
            VectorStore = await _vectorStoresService.Value.GetById(model.vectorStore),
            name = model.name
        };
        return data;
    }

    public async Task<ActionResult<int>> Validate(DataDto dataDto)
    {
        var asset = "null"; //await _assetsService.Value.GetModelByAssetId(dataDto.assetId);
        
        if (asset == null)
        {
            return new BadRequestObjectResult($"Could not find asset {dataDto.assetId}");
        }
        
        // //Check if stored file exists
        // if (!File.Exists(asset.storedPath))
        // {
        //     return new BadRequestObjectResult($"Could not find file {asset.storedPath} on storage");
        // }

        byte[]? fileBytes = null;
        try
        {
            // using (var fileStream = new FileStream(asset.storedPath, FileMode.Open, FileAccess.Read,
            //            FileShare.Read, bufferSize: 4096, useAsync: true))
            // {
            //     using (var memoryStream = new MemoryStream())
            //     {
            //         await fileStream.CopyToAsync(memoryStream);
            //         fileBytes = memoryStream.ToArray();
            //     }
            // }
        }
        catch (Exception ex)
        {
            // return new BadRequestObjectResult($"Error while reading file {asset.storedPath} on storage. {ex.Message}");
        }

        return new OkObjectResult("Data is valid");
    }

    public async Task<IActionResult> BuildDataModelFromDto(DataDto dataDto)
    {
        var dataModel = new DataModel
        {
            Id = dataDto.id,
            type = dataDto.type,
            split = dataDto.split
        };

        // if (dataDto.assetId != null)
        // {
        //     dataModel.Asset = await _assetsService.Value.GetModelByAssetId(dataDto.assetId);
        //
        //     if (dataModel.Asset == null)
        //     {
        //         return new BadRequestObjectResult(new { Message = $"Could not find asset {dataDto.assetId}" });
        //     }
        // }

        if (dataDto.embeddings != null)
        {
            var embeddingsEntity = await _embeddingsService.Value.GetById(dataDto.embeddings);

            if (embeddingsEntity == null)
            {
                return new BadRequestObjectResult(new { Message = $"Could not find embeddings {dataDto.embeddings}" });
            }

            dataModel.Embeddings = embeddingsEntity;
        }

        if (dataDto.vectorStore != null)
        {
            dataModel.VectorStore = await _vectorStoresService.Value.GetById(dataDto.vectorStore);

            if (dataModel.VectorStore == null)
            {
                return new BadRequestObjectResult(
                    new { Message = $"Could not find vector store {dataDto.vectorStore}" });
            }
        }

        return new OkObjectResult(dataModel);
    }

    /**
     * Called from Pipelines/Apply endpoint
     * ApplyPipelineDtoV1 includes Embeddings and VectorStores DTO
     *
     * Running RunApply for each data in applyPipelineDtoV1.data with Embeddings and VectorStores DTO
     */
    public async Task<IActionResult> PrepareApply(ApplyPipelineDtoV1 applyPipelineDto)
    {
        if (applyPipelineDto.data == null) return new BadRequestObjectResult("No data provided");

        var dataModels = new List<DataModel>();
        // create Data object and fill with fields
        if (applyPipelineDto.data != null)
        {
            foreach (var dataDto in applyPipelineDto.data)
            {
                var dataModelResult = await BuildDataModelFromDto(dataDto);

                if (dataModelResult is BadRequestObjectResult)
                {
                    return dataModelResult;
                }

                var dataModel = ((OkObjectResult)dataModelResult).Value as DataModel;

                // fix case where embeddings is null
                // 1. get from pipelines embeddings
                if (dataModel.Embeddings == null && applyPipelineDto.embeddings != null)
                {
                    var embeddingsEntity = await _embeddingsService.Value.GetById(applyPipelineDto.embeddings.id);
                    dataModel.Embeddings = embeddingsEntity;
                }

                // 2. get from pipelines model
                if (dataModel.Embeddings == null)
                {
                    var modelDto = applyPipelineDto.models?.FirstOrDefault();
                    if (modelDto != null)
                    {
                        dataModel.Embeddings = _embeddingsService.Value.CreateEmbeddingsEntityFromModelDto(modelDto);
                    }
                }

                if (dataModel.Embeddings == null)
                {
                    return new BadRequestObjectResult("No embeddings model found");
                }

                // fix case where vectorStore is null
                // 1. get from pipelines vector store
                if (dataModel.VectorStore == null && applyPipelineDto.stores != null)
                {
                    var vectorStoreEntity = await _vectorStoresService.Value.GetEntityById(applyPipelineDto.stores.id);
                    if (vectorStoreEntity != null)
                    {
                        dataModel.VectorStore = VectorStoreModel.FromEntity(vectorStoreEntity);
                    }
                }

                // 2. Get from ENV VAR or internal
                if (dataModel.VectorStore == null)
                {
                    dataModel.VectorStore = VectorStoreModel.GetEnvVarVectorStoreConfiguration() ??
                                            VectorStoreModel.GetInternalVectorStoreConfiguration();
                }

                dataModels.Add(dataModel);
            }
        }

        return await RunApply(dataModels.ToArray());
    }


    /**
     * Called from Data/Apply endpoint
     * DataDtoV1 dataDto includes Embeddings and VectorStores ids (not DTO)
     *
     * Trying to obtain DTO from DB by id, then running RunApply with Embeddings and VectorStores DTOs
     */
    public async Task<IActionResult> PrepareApply(DataDto dataDto)
    {
        // in case dataDto.embeddings/vectorStore is null, get default internal embeddings/vectorStore
        var result = await BuildDataModelFromDto(dataDto);

        if (result is BadRequestObjectResult)
        {
            return result;
        }

        var dataModel = ((OkObjectResult)result).Value as DataModel;

        return await RunApply(dataModel!);
    }

    /**
     * Receiving DataDto dataDto and Embeddings and VectorStores DTOs
     * In case Embeddings and VectorStores DTOs are null, try to get default Embeddings by given model
     * If model is null, then search in DB for pipelines that use this data
     *
     * Running DataApplyUseCase with EmbeddingsProvider and VectorStoreProvider
     */
    public async Task<IActionResult> RunApply(params DataModel[] dataModels)
    {
        foreach (var dataModel in dataModels)
        {
            var validationResult = await _dataApplyUseCase.Value.ValidateAsync(dataModel);

            if (validationResult is BadRequestObjectResult)
                return validationResult;

            await _dataApplyUseCase.Value.ExecuteAsync(dataModel);
        }

        return new OkObjectResult("Data applied");
    }
}