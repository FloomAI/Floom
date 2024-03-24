using Floom.Logs;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Model;
using Floom.Plugin.Context;
using Floom.Plugin.Loader;

namespace Floom.Pipeline.Stages.Response;

public interface IResponseStageHandler : IStageHandler { }


public class ResponseStageResultEvent : PipelineEvent
{
    public ResponseFormatterResult? ResultData { get; set; }
}

public class ResponseStageHandler : IResponseStageHandler
{
    private readonly ILogger _logger;
    public IPluginLoader PluginLoader { get; }
    public IPluginContextCreator PluginContextCreator { get; }

    public ResponseStageHandler(IPluginLoader pluginLoader, IPluginContextCreator pluginContextCreator)
    {
        _logger = FloomLoggerFactory.CreateLogger(GetType());
        PluginLoader = pluginLoader;
        PluginContextCreator = pluginContextCreator;
    }


    public async Task ExecuteAsync(PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Starting Response Stage Processing: {pipelineContext.Pipeline.Name}");

        // handle response stage
        await HandleFormatterAsync(pipelineContext);
        
        _logger.LogInformation($"Completed Response Stage Processing: {pipelineContext.Pipeline.Name}");
    }
    
    private async Task HandleFormatterAsync(PipelineContext pipelineContext)
    {
        // Logic to handle formatter
        var responseFormatter = pipelineContext.Pipeline.Response?.Format;

        // handle default formatter
        if (responseFormatter == null)
        {
            // get model stage result
            var modelStageResult = pipelineContext.GetEvents().OfType<ModelStageResultEvent>();
            
            var modelConnectorResult = modelStageResult.FirstOrDefault()?.Response;
            
            var responseStageResult = new ResponseFormatterResult
            {
                value = modelConnectorResult?.Data
            };
            
            // emit response stage result event
            pipelineContext.AddEvent(new ResponseStageResultEvent
            {
                ResultData = responseStageResult,
            });
        }
        else
        {
            foreach (var formatterPluginConfiguration in responseFormatter)
            {
                var formatterPlugin = PluginLoader.LoadPlugin(formatterPluginConfiguration.Package);
                
                if (formatterPlugin != null)
                {
                    var pluginContext = await PluginContextCreator.Create(formatterPluginConfiguration);

                    formatterPlugin.Initialize(pluginContext);
                    
                    var pluginResult = await formatterPlugin.Execute(pluginContext, pipelineContext);
                    
                    pipelineContext.AddEvent(new ResponseStageResultEvent
                    {
                        ResultData = pluginResult.Data as ResponseFormatterResult,
                    });
                }
                else
                {
                    _logger.LogError($"Formatter plugin not found: {formatterPluginConfiguration.Package}");
                }
            }
        }
    }
}