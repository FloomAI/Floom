using Floom.Model;
using Floom.Plugin.Context;
using Floom.Plugin.Loader;

namespace Floom.Pipeline.Stages.Model;

public interface IModelStageHandler : IStageHandler { }


public class ModelStageResultEvent : PipelineEvent
{
    public ModelConnectorResult Response { get; set; }
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
                    
                    pipelineContext.AddEvent(new ModelStageResultEvent
                    {
                        Response = pluginResult.Data as ModelConnectorResult,
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