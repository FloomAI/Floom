using Floom.Audit;
using Floom.Base;
using Floom.Context.Embeddings;
using Floom.Context.VectorStores;
using Floom.Data.Entities;
using Floom.Embeddings;
using Floom.Entities.AuditRow;
using Floom.Misc;
using Floom.Repository;
using Floom.VectorStores;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Data;

public interface IDataApplyUseCase : IUseCase<DataModel>
{
}

public class DataApplyUseCase : IDataApplyUseCase
{
    private readonly FloomAuditService _auditService;
    private readonly Lazy<IEmbeddingsService> _embeddingsService;
    private readonly Lazy<IVectorStoresService> _vectorStoresService;
    private readonly IRepository<DataEntity> _repository;
    private readonly IServiceProvider _serviceProvider;

    public DataApplyUseCase(
        IServiceProvider serviceProvider,
        IRepositoryFactory repositoryFactory,
        Lazy<IEmbeddingsService> embeddingsService,
        Lazy<IVectorStoresService> vectorStoresService)
    {
        _serviceProvider = serviceProvider;
        _repository = repositoryFactory.Create<DataEntity>("data");
        _auditService = serviceProvider.GetRequiredService<FloomAuditService>();
        _embeddingsService = embeddingsService;
        _vectorStoresService = vectorStoresService;
    }

    async Task<ActionResult> ReturnError(string errorMessage)
    {
        return new BadRequestObjectResult(new { Message = errorMessage });
    }

    private VectorStoreProvider GetVectorStoreProvider(DataModel dataModel)
    {
        // var vectorStoreProvider = VectorStoresFactory.GetFactory(_serviceProvider).Create(dataModel.VectorStore);
        // return vectorStoreProvider;
        return null;
    }

    public async Task<IActionResult> ValidateAsync(DataModel dataModel)
    {
        if (dataModel.Asset == null)
        {
            return await ReturnError($"Could not find asset {dataModel.Asset}");
        }

        //Check if stored file exists
        if (!File.Exists(dataModel.Asset.storedPath))
        {
            return await ReturnError($"Could not find file {dataModel.Asset.storedPath} on storage");
        }

        // var embeddingsProvider = EmbeddingsFactory.GetFactory(_serviceProvider).Create(
            // dataModel.Embeddings.vendor, dataModel.Embeddings.apiKey, dataModel.Embeddings.model);

        // var isModelValid = await embeddingsProvider.ValidateModelAsync();

        // if (isModelValid is BadRequestObjectResult)
        // {
            // return isModelValid;
        // }

        var vectorStoreProvider = GetVectorStoreProvider(dataModel);

        var isVectorStoreValid = await vectorStoreProvider.HealthCheck();

        if (isVectorStoreValid is not OkObjectResult)
        {
            return isVectorStoreValid;
        }

        return new OkObjectResult(new { Message = $"Embeddings Model {dataModel.Embeddings.model} Validated" });
    }

    public async Task<IActionResult> ExecuteAsync(DataModel dataModel)
    {
        var splitText = new List<string>();

        //Type: File
        if (dataModel.type == DataType.File)
        {
            #region Read the file

            byte[]? fileBytes = null;
            try
            {
                await using var fileStream = new FileStream(dataModel.Asset.storedPath, FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read, bufferSize: 4096, useAsync: true);
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                return await ReturnError(
                    $"Error while reading file {dataModel.Asset.storedPath} on storage. {ex.Message}");
            }

            #endregion

            #region Parse File (Read content)

            var documentManager = new DocumentManager();

            ExtractionMethod extractionMethod = ExtractionMethod.ByPages;
            switch (dataModel.split)
            {
                case SplitType.Pages:
                    extractionMethod = ExtractionMethod.ByPages;
                    break;
                case SplitType.Paragraphs:
                    extractionMethod = ExtractionMethod.ByParagraphs;
                    break;
                // case SplitType.Pages:
                //     extractionMethod = ExtractionMethod.ByTOC;
                //     break;
            }

            //TODO: Change 500
            splitText = await documentManager.ExtractTextAsync(dataModel.Asset.extension, fileBytes,
                extractionMethod,
                maxCharactersPerItem: null);

            #endregion

            var embeddingsVectors = new List<List<float>>();

            #region Send split content to EmbeddingsProvider (OpenAI) (dont worry about conf at first)

            //# TODO: Temporary - Reduce to 3 pages!!!!!!!!!!
            //splitText = splitText;
            //splitText = splitText.GetRange(0, 20);

            // var embeddingsProvider = EmbeddingsFactory.GetFactory(_serviceProvider)
                // .Create(dataModel.Embeddings.vendor, dataModel.Embeddings.apiKey,
                    // dataModel.Embeddings.model);
            // embeddingsVectors = await embeddingsProvider.GetEmbeddingsAsync(splitText);
            embeddingsVectors = new List<List<float>>();
            #endregion


            //# Store Embeddings in MDB (Fast switch)

            #region Set VectorStore

            //Data.VectorStore -> ENV VAR -> Internal Milvus

            //If dataDto.VS not provided, use ENV_VAR.VS, if not, use Internal Milvus

            var vectorStoreProvider = GetVectorStoreProvider(dataModel);

            #endregion

            #region Store Embeddings in VectorStore (Cosine Similarity)

            // vectorStoreProvider.CollectionName =
                // VectorStores.Utils.GetCollectionName(dataModel.Id, embeddingsProvider.GetModelName());
            // for later improvent: use indication on Data Entity when last time embeddings were inserted
            await vectorStoreProvider.Prepare();
            await vectorStoreProvider.CreateAndInsertVectors(splitText, embeddingsVectors);

            #endregion

            var dataEntity = new DataEntity
            {
                name = dataModel.Id,
                assetId = dataModel.Asset.assetId,
                type = dataModel.type,
                split = dataModel.split
            };

            if (dataModel.Embeddings != null)
            {
                // Save dataModel.Embeddings if needed
                await _embeddingsService.Value.Insert(dataModel.Embeddings);

                // Save dataModel.EmbeddingsEntity name on DataEntity
                dataEntity.embeddings = dataModel.Embeddings.name;
            }

            if (dataModel.VectorStore != null)
            {
                // Save dataModel.VectorStoreEntity if needed
                await _vectorStoresService.Value.Insert(dataModel.VectorStore);
                // Save dataModel.EmbeddingsEntity name on DataEntity
                dataEntity.vectorStore = dataModel.VectorStore.name;
            }

            await _repository.DeleteByName(dataModel.Id);
            await _repository.Insert(dataEntity);
        }

        //# Audit   
        _auditService.Insert(
            action: AuditAction.Create,
            objectType: "data",
            objectId: dataModel.Id
        );

        return new OkResult();
    }
}