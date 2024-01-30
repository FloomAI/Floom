using Floom.LLMs;
using Floom.Model;
using Floom.Plugin;

namespace Floom.Pipeline.Model;

public interface IModelStageHandler : IStageHandler { }


public class ModelConnectorResultEvent : PipelineEvent
{
    public PromptResponse Response { get; set; }
}

public class ModelStageHandler : IModelStageHandler
{
    private readonly ILogger<ModelStageHandler> _logger;

    public ModelStageHandler(ILogger<ModelStageHandler> logger, IPluginContextCreator pluginContextCreator, IPluginLoader pluginLoader)
    {
        _logger = logger;
        PluginContextCreator = pluginContextCreator;
        PluginLoader = pluginLoader;
    }

    public IPluginContextCreator PluginContextCreator { get; }
    public IPluginLoader PluginLoader { get; }

    public async Task ExecuteAsync(PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Starting Model Stage Processing: {pipelineContext.Pipeline.Name}");

        // handle model stage
        await HandleConnectorAsync(pipelineContext);
        
        _logger.LogInformation($"Completed Model Stage Processing: {pipelineContext.Pipeline.Name}");
    }
    
    private async Task HandleConnectorAsync(PipelineContext pipelineContext)
    {
        // Logic to handle prompt template
        if(pipelineContext.Pipeline.Model != null)
        {
            _logger.LogInformation("Model Stage: Handling model connector.");
            
            foreach (var modelPluginConfiguration in pipelineContext.Pipeline.Model)
            {
                var connectorPlugin = PluginLoader.LoadPlugin(modelPluginConfiguration.Package);
                
                if(connectorPlugin != null)
                {
                    var pluginContext = await PluginContextCreator.Create(modelPluginConfiguration);
                    
                    connectorPlugin.Initialize(pluginContext);
                    
                    var pluginResult = await connectorPlugin.Execute(pluginContext, pipelineContext);
                    
                    if(pluginResult.Success)
                    {
                        pipelineContext.AddEvent(new ModelConnectorResultEvent
                        {
                            Response = pluginResult.ResultData as PromptResponse,
                        });
                    }
                    else
                    {
                        _logger.LogError("Error executing model connector plugin: {Message}", pluginResult.Message);
                    }
                }
                else
                {
                    _logger.LogError("Error loading model connector plugin: {Package}", modelPluginConfiguration.Package);
                }
            }
        }
        else
        {
            _logger.LogError("Error loading model connector plugin for pipeline: {Package}", pipelineContext.Pipeline.Name);
        }
    }
}