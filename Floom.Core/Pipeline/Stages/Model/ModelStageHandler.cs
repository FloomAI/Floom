using Floom.Model;
using Floom.Plugin;
using Floom.Plugin.Context;
using Floom.Plugin.Loader;

namespace Floom.Pipeline.StageHandler.Model;

public interface IModelStageHandler : IStageHandler { }


public class ModelConnectorResultEvent : PipelineEvent
{
    public FloomPromptResponse Response { get; set; }
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
            
            foreach (var modelConnectorPluginContext in pipelineContext.Pipeline.Model)
            {
                var connectorPlugin = PluginLoader.LoadPlugin(modelConnectorPluginContext.Package);
                
                if(connectorPlugin != null)
                {
                    connectorPlugin.Initialize(modelConnectorPluginContext);
                    
                    var pluginResult = await connectorPlugin.Execute(modelConnectorPluginContext, pipelineContext);
                    
                    pipelineContext.AddEvent(new ModelConnectorResultEvent
                    {
                        Response = pluginResult.Data as FloomPromptResponse,
                    });
                }
                else
                {
                    _logger.LogError("Error loading model connector plugin: {Package}", modelConnectorPluginContext.Package);
                }
            }
        }
        else
        {
            _logger.LogError("Error loading model connector plugin for pipeline: {Package}", pipelineContext.Pipeline.Name);
        }
    }
}