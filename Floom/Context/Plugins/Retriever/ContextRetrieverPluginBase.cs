using Floom.Audit;
using Floom.Context.Embeddings;
using Floom.Context.VectorStores;
using Floom.Data;
using Floom.Embeddings;
using Floom.Entities.AuditRow;
using Floom.Pipeline;
using Floom.Pipeline.Prompt;
using Floom.Plugin;
using Floom.Utils;
using Floom.VectorStores;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Context;

public abstract class ContextRetrieverPluginBase : FloomPluginBase
{
    private ContextRetrieverPluginConfigBase _config;

    public ContextRetrieverPluginBase()
    {
    }

    public abstract string GetDocumentType();
    
    public override void Initialize(PluginContext context)
    {
        _logger.LogInformation($"Initializing {GetType()}");

        // Initialize settings with specific plugin settings class
        _config = new ContextRetrieverPluginConfigBase(context.Configuration.Configuration);
        
        // check whether plugin configuration provides embeddings and vector store settings
        if(_config.VectorStore == null)
        {
            // Try to get Env Var Vector Store configurations
            _config.VectorStore = VectorStoreConfiguration.GetEnvVarVectorStoreConfiguration();
            
            // in case no vector store config is found, get the default configurations from the plugin manifest
            if(_config.VectorStore == null)
            {
                if (context.Manifest.Parameters.TryGetValue("vectorStore", out var vectorstore))
                {
                    _config.VectorStore = new VectorStoreConfiguration(vectorstore.DefaultValue);
                }
            }
        }

        if (_config.Embeddings == null)
        {
            if (context.Manifest.Parameters.TryGetValue("embeddings", out var embeddings))
            {
                _config.Embeddings = new EmbeddingsConfiguration(embeddings.DefaultValue);
            }
        }
    }

    public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing ContextRetrieverPlugin {pluginContext.Package}");
        
        var floomAsset = await FloomAssetsRepository.Instance.GetAssetById(_config.AssetId);

        //List of Similarity Search Results (from all Datas)
        var vectorSearchResults = new List<VectorSearchResult>();
        var mergedResults = string.Empty;
        
        //Iterate Data
        #region Get Query Embeddings

        EmbeddingsProvider? embeddingsProvider = GetEmbeddingsProvider(pipelineContext, _config.Embeddings);

        var queryEmbeddings = new List<List<float>>();
        
        queryEmbeddings = await embeddingsProvider.GetEmbeddingsAsync(new List<string>()
        {
            pipelineContext.Request.input
        }
        );

        #endregion
        
        #region Similarity Search

        _logger.LogInformation("Similarity Search");

        if (_config.VectorStore != null)
        {
            var vectorStoreProvider = VectorStoresFactory.Create(_config.VectorStore);
            vectorStoreProvider.CollectionName = Floom.VectorStores.Utils.GetCollectionName(floomAsset.AssetId, embeddingsProvider.GetModelName());
            List<VectorSearchResult> results = await vectorStoreProvider.Search(
                queryEmbeddings.First(),
                topResults: 3
            );
            vectorSearchResults.AddRange(results);
        }

        foreach (var vectorSearchResult in vectorSearchResults.Take(3))
        {
            mergedResults += $"{vectorSearchResult.text}. \n";
        }

        // Obtaining PromptTemplateResultEvent from pipelineContext.Events
        
        var promptTemplateResultEvent = pipelineContext.GetEvents().FirstOrDefault(x => x.GetType() == typeof(PromptTemplateResultEvent)) as PromptTemplateResultEvent;
        var promptRequest = promptTemplateResultEvent.ResultData;
        
        //Add Data to the System's Prompt (Make sure it ends with dot.)
        promptRequest.system += $"Answer directly and shortly, no additions, just the answer itself. ";

        if (mergedResults.Length > 0)
        {
            promptRequest.system += $" \n The documentation section is: '{mergedResults}' "; //Documentation supplied
        }

        #endregion

        return new PluginResult()
        {
            Success = true,
            ResultData = promptRequest
        };
    }

    public override async Task HandleEvent(string EventName, PluginContext pluginContext,
        PipelineContext pipelineContext)
    {
        Initialize(pluginContext);
        
        _logger.LogInformation($"Handling event {EventName} for plugin {pluginContext.Package}");
        
        var floomAsset = await FloomAssetsRepository.Instance.GetAssetById(_config.AssetId);
        var actionResult = await GenerateAndStoreEmbeddingsFromFile(pipelineContext, floomAsset, _config.VectorStore,
            _config.Embeddings);
        
        _logger.LogInformation($"Handling event {EventName} for plugin {pluginContext.Package} finished with result {actionResult}");
    }
    
    private EmbeddingsProvider GetEmbeddingsProvider(PipelineContext pipelineContext, EmbeddingsConfiguration embeddingsConfiguration)
    {
        EmbeddingsProvider? embeddingsProvider = null;
        
        foreach (var pipelineModelConnector in pipelineContext.Pipeline.Model)
        {
            if(pipelineModelConnector.Package == "floom/model/connector/openai")
            {
                var modelApiKey = pipelineModelConnector.Configuration["apikey"] as string;
                embeddingsProvider = EmbeddingsFactory.Create(embeddingsConfiguration.Vendor, modelApiKey, _config.Embeddings.Model);
                break;
            }
        }

        return embeddingsProvider;
    }
        
    // Change Execute to receive:
    // 1. string storedPath
    // 2. VectorStoreConfiguration vectorStoreConfiguration
    // 3. EmbeddingsConfiguration embeddingsConfiguration
    
    private async Task<IActionResult> GenerateAndStoreEmbeddingsFromFile(
        PipelineContext pipelineContext,
        FloomAsset floomAsset,
        VectorStoreConfiguration vectorStoreConfiguration,
        EmbeddingsConfiguration embeddingsConfiguration)
    {
        _logger.LogInformation($"Generating and storing embeddings for file {floomAsset.StoredPath}");
        
        var splitText = new List<string>();

        #region Read the file

        byte[]? fileBytes = null;
        try
        {
            _logger.LogInformation($"Reading file {floomAsset.StoredPath} on storage");

            await using var fileStream = new FileStream(floomAsset.StoredPath, FileMode.Open,
                FileAccess.Read,
                FileShare.Read, bufferSize: 4096, useAsync: true);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            fileBytes = memoryStream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error while reading file {floomAsset.StoredPath} on storage. {ex.Message}");
            return new BadRequestObjectResult(new { Message = $"Error while reading file {floomAsset.StoredPath} on storage. {ex.Message}" });
        }

        #endregion

        #region Parse File (Read content)

        _logger.LogInformation($"Parsing file {floomAsset.StoredPath}");
        
        var documentManager = new DocumentManager();

        ExtractionMethod extractionMethod = ExtractionMethod.ByPages;
        // switch (dataModel.split)
        // {
            // case SplitType.Pages:
            //     extractionMethod = ExtractionMethod.ByPages;
            //     break;
            // case SplitType.Paragraphs:
            //     extractionMethod = ExtractionMethod.ByParagraphs;
            //     break;
            // case SplitType.Pages:
            //     extractionMethod = ExtractionMethod.ByTOC;
            //     break;
        // }

        _logger.LogInformation($"Extracting text from file {floomAsset.StoredPath}");
        
        splitText = await documentManager.ExtractTextAsync(GetDocumentType(), fileBytes,
            extractionMethod,
            maxCharactersPerItem: null);

        #endregion

        var embeddingsVectors = new List<List<float>>();

        #region Send split content to EmbeddingsProvider (OpenAI) (dont worry about conf at first)
        
        //# TODO: Temporary - Reduce to 3 pages
        //splitText = splitText.GetRange(0, 20);
        
        // Iterate over plugin configuration
        // by package, get the model connector (for now support only openai connector)
        
        _logger.LogInformation($"Getting embeddings from Pipeline Model Connectors");

        var embeddingsProvider = GetEmbeddingsProvider(pipelineContext, embeddingsConfiguration);
        
        _logger.LogInformation($"Getting embeddings from Pipeline Model Connectors finished");
        
        embeddingsVectors = await embeddingsProvider.GetEmbeddingsAsync(splitText);
        
        #endregion


        //# Store Embeddings in MDB (Fast switch)

        #region Set VectorStore
        
        _logger.LogInformation($"Getting VectorStore from VectorStoreConfiguration");
        
        var vectorStoreProvider = VectorStoresFactory.Create(vectorStoreConfiguration);
        
        #endregion

        #region Store Embeddings in VectorStore (Cosine Similarity)
        
        // get storePath hash, last 4 string characters
        
        _logger.LogInformation($"Storing embeddings in VectorStore");
        
        vectorStoreProvider.CollectionName = Floom.VectorStores.Utils.GetCollectionName(floomAsset.AssetId, embeddingsProvider.GetModelName());
        // for later improvement: use indication on Data Entity when last time embeddings were inserted
        await vectorStoreProvider.Prepare();
        await vectorStoreProvider.CreateAndInsertVectors(splitText, embeddingsVectors);

        #endregion
        
        //# Audit   
        FloomAuditService.Instance.Insert(
            action: AuditAction.Create,
            objectType: "data",
            objectId: floomAsset.AssetId
        );

        return new OkResult();
    }
}