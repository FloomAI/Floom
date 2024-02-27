using Floom.Assets;
using Floom.Audit;
using Floom.Data;
using Floom.Pipeline;
using Floom.Pipeline.StageHandler.Prompt;
using Floom.Plugin.Base;
using Floom.Plugin.Context;
using Floom.Plugins.Prompt.Context.Embeddings;
using Floom.Plugins.Prompt.Context.VectorStores;
using Floom.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static System.String;

namespace Floom.Plugins.Prompt.Context.Retriever;

public abstract class ContextRetrieverPluginBase : FloomPluginBase
{
    private ContextRetrieverPluginConfigBase _config;

    public ContextRetrieverPluginBase()
    {
    }
    
    public abstract Task<List<string>> ParseFile(byte[] fileBytes,
        ExtractionMethod extractionMethod, int? maxCharactersPerItem);
    
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
        
        //List of Similarity Search Results (from all Datas)
        var vectorSearchResults = new List<VectorSearchResult>();
        var mergedResults = Empty;
        
        //Iterate Data
        #region Get Query Embeddings

        EmbeddingsProvider? embeddingsProvider = GetEmbeddingsProvider(pipelineContext, _config.Embeddings);

        var embeddingsResult = await embeddingsProvider.GetEmbeddingsAsync(new List<string>()
        {
            pipelineContext.Request.input
        });

        if (embeddingsResult.Success == false)
        {
            _logger.LogError($"Error while getting embeddings from Pipeline Model Connectors. {embeddingsResult.Message}");
            
            return new PluginResult()
            {
                Success = false
            };
        }

        var queryEmbeddings = embeddingsResult.Data;

        #endregion
        
        #region Similarity Search

        _logger.LogInformation("Running Similarity Search");

        if (_config.VectorStore != null)
        {
            var vectorStoreProvider = VectorStoresFactory.Create(_config.VectorStore);
            vectorStoreProvider.CollectionName = VectorStores.Utils.GetCollectionName(pipelineContext.PipelineName, pipelineContext.Pipeline.UserId);
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
            Data = promptRequest
        };
    }

    public override async Task HandleEvent(string EventName, PluginContext pluginContext,
        PipelineContext pipelineContext)
    {
        Initialize(pluginContext);
        
        _logger.LogInformation($"Handling event {EventName} for plugin {pluginContext.Package}");
        
        var actionResult = await GenerateAndStoreEmbeddingsFromFile(pipelineContext, _config.AssetsIds, _config.VectorStore,
            _config.Embeddings);
        
        if (actionResult is OkResult)
        {
            _logger.LogInformation($"Handling event {EventName} for plugin {pluginContext.Package} finished with result success");
        }
        else
        {
            _logger.LogError($"Handling event {EventName} for plugin {pluginContext.Package} finished with result failure");
        }
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
        List<string> assetsIds,
        VectorStoreConfiguration vectorStoreConfiguration,
        EmbeddingsConfiguration embeddingsConfiguration)
    {
        _logger.LogInformation($"Preparing for generating and storing embeddings");

        var vectorStoreProvider = VectorStoresFactory.Create(vectorStoreConfiguration);
        vectorStoreProvider.CollectionName = VectorStores.Utils.GetCollectionName(pipelineContext.PipelineName, pipelineContext.Pipeline.UserId);
        await vectorStoreProvider.Prepare();
        var splitText = new List<string>();
        var embeddingsVectors = new List<List<float>>();

        foreach (var assetId in assetsIds)
        {
            _logger.LogInformation($"Generating and storing embeddings for file {assetId}");

            var floomAsset = await FloomAssetsRepository.Instance.GetAssetById(assetId);
            
            #region Read the file

            var floomEnvironment = Environment.GetEnvironmentVariable("FLOOM_ENVIRONMENT");
            _logger.LogInformation($"Floom Environment: {floomEnvironment}");
            
            byte[]? fileBytes = null;

            if(floomEnvironment == "local")
            {
                try
                {
                    _logger.LogInformation($"Reading file {floomAsset.StoredPath} on storage");
                    fileBytes = await FileUtils.ReadFileAsync(floomAsset);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while reading file {floomAsset.StoredPath} on storage. {ex.Message}");
                    return new BadRequestObjectResult(new { Message = $"Error while reading file {floomAsset.StoredPath} on storage. {ex.Message}" });
                }
            }
            else if(floomEnvironment == "cloud")
            {
                try
                {
                    _logger.LogInformation($"Reading file {floomAsset.StoredPath} from S3");
                    fileBytes = await FileUtils.ReadFileCloudAsync(floomAsset);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error while reading file {floomAsset.StoredPath} on storage. {ex.Message}");
                    return new BadRequestObjectResult(new { Message = $"Error while reading file {floomAsset.StoredPath} on storage. {ex.Message}" });
                }
            }
            
            #endregion
            
            #region Parse File (Read content)

            _logger.LogInformation($"Parsing file {floomAsset.StoredPath}");
        
            ExtractionMethod extractionMethod = ExtractionMethod.ByPages;
        
            _logger.LogInformation($"Extracting text from file {floomAsset.StoredPath}");
        
            var pagesContent = await ParseFile(fileBytes, extractionMethod, maxCharactersPerItem: null);
            
            splitText.AddRange(pagesContent);
            
            #endregion

            #region Send split content to EmbeddingsProvider (OpenAI) (dont worry about conf at first)
        
            //# TODO: Temporary - Reduce to 3 pages
            //splitText = splitText.GetRange(0, 20);
        
            _logger.LogInformation($"Getting embeddings from Pipeline Model Connectors");

            var embeddingsProvider = GetEmbeddingsProvider(pipelineContext, embeddingsConfiguration);
        
            _logger.LogInformation($"Getting embeddings from Pipeline Model Connectors finished");
        
            var embeddingsResult = await embeddingsProvider.GetEmbeddingsAsync(splitText);

            if (embeddingsResult.Success == false)
            {
                _logger.LogError($"Error while getting embeddings from Pipeline Model Connectors. {embeddingsResult.Message}");
                return new BadRequestResult();
            }
        
            #endregion
            
            embeddingsVectors.AddRange(embeddingsResult.Data);
        }
        
        #region Store Embeddings in VectorStore (Cosine Similarity)
            
        _logger.LogInformation($"Storing embeddings in VectorStore");

        await vectorStoreProvider.CreateAndInsertVectors(splitText, embeddingsVectors);

        #endregion
            
        FloomAuditService.Instance.Insert(
            action: AuditAction.Create,
            objectType: "pipeline",
            objectId: pipelineContext.Pipeline.Id
        );
        
        return new OkResult();
    }
}