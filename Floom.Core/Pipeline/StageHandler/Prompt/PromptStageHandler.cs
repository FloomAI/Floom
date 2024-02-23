using Floom.Model;
using Floom.Plugin.Context;
using Floom.Plugin.Loader;
using Floom.Utils;

namespace Floom.Pipeline.StageHandler.Prompt;

public interface IPromptStageHandler : IStageHandler { }

public class PromptTemplateResultEvent : PipelineEvent
{
    public FloomPromptRequest? ResultData { get; set; }
}

public class PromptContextResultEvent : PipelineEvent
{
    public FloomPromptRequest? ResultData { get; set; }
}

public class PromptValidationResultEvent : PipelineEvent
{
    public object? ResultData { get; set; }
}

public class PromptStageHandler : IPromptStageHandler
{
    private readonly ILogger<PromptStageHandler> _logger;
    
    public PromptStageHandler(ILogger<PromptStageHandler> logger, IPluginLoader pluginLoader, IPluginContextCreator pluginContextCreator)
    {
        _logger = logger;
        PluginLoader = pluginLoader;
        PluginContextCreator = pluginContextCreator;
    }

    public IPluginLoader PluginLoader { get; }

    public async Task ExecuteAsync(PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Starting Prompt Stage Processing: {pipelineContext.Pipeline.Name}");
        
        // handle prompt template
        await HandleTemplateAsync(pipelineContext);
        
        // handle prompt context
        await HandleContextAsync(pipelineContext);
        
        // handle prompt optimization
        await HandleOptimizationAsync(pipelineContext);

        // handle prompt validation
        await HandleValidationAsync(pipelineContext);
        
        _logger.LogInformation($"Completed Prompt Stage Processing: {pipelineContext.Pipeline.Name}");
    }

    private async Task HandleTemplateAsync(PipelineContext pipelineContext)
    {
        // Logic to handle prompt template
        var promptTemplateConfiguration = pipelineContext.Pipeline.Prompt?.Template;

        if (promptTemplateConfiguration != null)
        {
            _logger.LogInformation("Prompt Stage: Handling prompt template.");
            
            var templatePlugin = PluginLoader.LoadPlugin(promptTemplateConfiguration.Package);

            if (templatePlugin != null)
            {
                var pluginContext = await PluginContextCreator.Create(promptTemplateConfiguration);
                
                templatePlugin.Initialize(pluginContext);
                
                var pluginResult = await templatePlugin.Execute(pluginContext, pipelineContext);
            
                // Emit an event
                if (pluginResult.Success)
                {
                    pipelineContext.AddEvent(new PromptTemplateResultEvent 
                    { 
                        ResultData = pluginResult.Data as FloomPromptRequest,
                    });
                }
            }
        }
        else
        {
            _logger.LogInformation("Prompt Stage: No prompt template found.");
            // Emit an empty FloomPromptRequest
            pipelineContext.AddEvent(new PromptTemplateResultEvent 
            { 
                ResultData = new FloomPromptRequest()
                {
                    user = pipelineContext.Request.input,
                },
            });
        }

        if (pipelineContext.Request.file != null)
        {
            var promptTemplateResultEvents = pipelineContext.GetEvents().OfType<PromptTemplateResultEvent>();
            promptTemplateResultEvents.ToList().ForEach(e => e.ResultData.file = FileUtils.ConvertIFormFileToByteArrayAsync(pipelineContext.Request.file).Result );
        }
    }

    private async Task HandleContextAsync(PipelineContext pipelineContext)
    {
        // Logic to handle prompt context
        var promptContextConfiguration = pipelineContext.Pipeline.Prompt?.Context;

        if (promptContextConfiguration != null)
        {
            if(promptContextConfiguration.Any())
            {
                _logger.LogInformation("Prompt Stage: Handling prompt context.");
            }
            foreach (var contextPluginConfiguration in promptContextConfiguration)
            {
                var contextPlugin = PluginLoader.LoadPlugin(contextPluginConfiguration.Package);
    
                if (contextPlugin != null)
                {
                    var pluginContext = await PluginContextCreator.Create(contextPluginConfiguration);
                
                    contextPlugin.Initialize(pluginContext);
                
                    var pluginResult = await contextPlugin.Execute(pluginContext, pipelineContext);
            
                    // Emit an event
                    if (pluginResult.Success)
                    {
                        pipelineContext.AddEvent(new PromptContextResultEvent 
                        { 
                            ResultData = pluginResult.Data as FloomPromptRequest,
                        });
                    }
                }
            }
        }
    }

    private async Task HandleOptimizationAsync(PipelineContext pipelineContext)
    {
        // Logic to handle prompt optimization
        var promptOptimizationConfiguration = pipelineContext.Pipeline.Prompt?.Optimization;

        if (promptOptimizationConfiguration != null && promptOptimizationConfiguration.Any())
        {
            _logger.LogInformation("Prompt Stage: Handling prompt optimization.");
        }
    }

    private async Task HandleValidationAsync(PipelineContext pipelineContext)
    {
        // Logic to handle prompt validation
        var promptValidationConfiguration = pipelineContext.Pipeline.Prompt?.Validation;

        if (promptValidationConfiguration != null)
        {
            if(promptValidationConfiguration.Any())
            {
                _logger.LogInformation("Prompt Stage: Handling prompt validation.");
            }
            
            foreach (var validationPluginConfiguration in promptValidationConfiguration)
            {
                var validationPlugin = PluginLoader.LoadPlugin(validationPluginConfiguration.Package);
    
                if (validationPlugin != null)
                {
                    var pluginContext = await PluginContextCreator.Create(validationPluginConfiguration);
                
                    validationPlugin.Initialize(pluginContext);
                
                    var pluginResult = await validationPlugin.Execute(pluginContext, pipelineContext);
            
                    // Emit an event
                    if (pluginResult.Success)
                    {
                        pipelineContext.AddEvent(new PromptValidationResultEvent());
                    }
                }
            }
        }
    }

    public IPluginContextCreator PluginContextCreator { get; }
}