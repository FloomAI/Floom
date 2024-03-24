using Floom.Assets;
using Floom.Audit;
using Floom.Pipeline;
using Floom.Pipeline.Stages.Prompt;
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
    private VectorStoreConfiguration? _vectorStore { get; set; }
    private EmbeddingsProvider? _embeddingsProvider { get; set; }
    
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
        // Get Vector Store settings from environment variables
        _vectorStore = VectorStoreConfiguration.GetEnvVarVectorStoreConfiguration();
    }
    
    public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing ContextRetrieverPlugin {pluginContext.Package}");
        
        //List of Similarity Search Results (from all Datas)
        var vectorSearchResults = new List<VectorSearchResult>();
        var mergedResults = Empty;
        
        var promptTemplateResultEvent = pipelineContext.GetEvents().FirstOrDefault(x => x.GetType() == typeof(PromptTemplateResultEvent)) as PromptTemplateResultEvent;
        var promptTemplateResult = promptTemplateResultEvent.ResultData;
        var userPrompt = promptTemplateResult.UserPrompt;
        
        //Iterate Data
        #region Get Query Embeddings

        var embeddingsProvider = GetEmbeddingsProvider(pipelineContext);
        
        var embeddingsResult = await embeddingsProvider.GetEmbeddingsAsync(new List<string>()
        {
            userPrompt
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

        if (_vectorStore != null)
        {
            var vectorStoreProvider = VectorStoresFactory.Create(_vectorStore);
            vectorStoreProvider.CollectionName = VectorStores.Utils.GetCollectionName(pipelineContext);
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

        var promptContextResult = new PromptContextResult
        {
            Context = ""
        };
        
        promptContextResult.Context += $"Answer directly and shortly, no additions, just the answer itself. ";

        if (mergedResults.Length > 0)
        {
            promptContextResult.Context += $" \n The documentation section is: '{mergedResults}' "; //Documentation supplied
        }

        #endregion

        return new PluginResult()
        {
            Success = true,
            Data = promptContextResult
        };
    }

    public override async Task HandleEvent(string EventName, PluginContext pluginContext,
        PipelineContext pipelineContext)
    {
        Initialize(pluginContext);
        
        _logger.LogInformation($"Handling event {EventName} for plugin {pluginContext.Package}");
        
        var actionResult = await GenerateAndStoreEmbeddingsFromFile(pipelineContext, _config.AssetsIds, _vectorStore);
        
        if (actionResult is OkResult)
        {
            _logger.LogInformation($"Handling event {EventName} for plugin {pluginContext.Package} finished with result success");
        }
        else
        {
            _logger.LogError($"Handling event {EventName} for plugin {pluginContext.Package} finished with result failure");
        }
    }
    
    private EmbeddingsProvider? GetEmbeddingsProvider(PipelineContext pipelineContext)
    {
        if(_embeddingsProvider == null)
        {
            foreach (var modelConnectorPluginContext in pipelineContext.Pipeline.Model)
            {
                var modelConnectorParams = modelConnectorPluginContext.Configuration.Configuration;
                modelConnectorParams.TryGetValue("apikey", out var modelApiKey);
                modelConnectorParams.TryGetValue("embeddingsModel", out var embeddingsModel);
                var modelApiKeyString =  modelApiKey as string;
                var embeddingsModelString = embeddingsModel as string;
                if(embeddingsModelString == null && modelConnectorPluginContext.Manifest != null)
                {
                    modelConnectorPluginContext.Manifest.Parameters.TryGetValue("embeddingsModel", out var embeddingsModelParameter);
                    var embeddingsModelDefaultValue = embeddingsModelParameter.DefaultValue as Dictionary<object, object>;
                    embeddingsModelString = embeddingsModelDefaultValue["value"] as string;
                }
                var modelConnectorPackage = modelConnectorPluginContext.Package;
                // get vendor by the name after the 'connector'
                // for example, if the package is 'floom/model/connector/openai', then the vendor is 'openai'
                var modelConnectorVendor = modelConnectorPackage.Split("/").Last();
                _embeddingsProvider = EmbeddingsFactory.Create(modelConnectorVendor, modelApiKeyString, embeddingsModelString);
            }
        }

        if (_embeddingsProvider == null)
        {
            _logger.LogError("Embeddings configuration is not available for pipeline's model connector plugin");
        }
        
        return _embeddingsProvider;
    }
        
    // Change Execute to receive:
    // 1. string storedPath
    // 2. VectorStoreConfiguration vectorStoreConfiguration
    // 3. EmbeddingsConfiguration embeddingsConfiguration
    
    private async Task<IActionResult> GenerateAndStoreEmbeddingsFromFile(
        PipelineContext pipelineContext,
        List<string> assetsIds,
        VectorStoreConfiguration vectorStoreConfiguration)
    {
        _logger.LogInformation($"Preparing for generating and storing embeddings");
        var embeddingsProvider = GetEmbeddingsProvider(pipelineContext);
        var vectorStoreProvider = VectorStoresFactory.Create(vectorStoreConfiguration);
        
        vectorStoreProvider.CollectionName = VectorStores.Utils.GetCollectionName(pipelineContext);
        await vectorStoreProvider.Prepare(EmbeddingsDimensionResolver.GetDimension(embeddingsProvider));
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
            
            // remove any empty strings or have " "
            pagesContent = pagesContent.Where(x => !IsNullOrWhiteSpace(x)).ToList();
            
            splitText.AddRange(pagesContent);
            
            #endregion

            #region Send split content to EmbeddingsProvider (OpenAI) (dont worry about conf at first)
        
            //# TODO: Temporary - Reduce to 3 pages
            //splitText = splitText.GetRange(0, 20);
        
            _logger.LogInformation($"Getting embeddings from Pipeline Model Connectors");

        
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