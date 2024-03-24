using Floom.Plugin.Base;
using Floom.Plugin.Context;
using Floom.Plugin.Loader;
using Floom.Utils;

namespace Floom.Pipeline.Stages.Prompt;

public interface IPromptStageHandler : IStageHandler { }


public class PromptStageHandler : IPromptStageHandler
{
    private readonly ILogger<PromptStageHandler> _logger;
    public IPluginLoader PluginLoader { get; }
    public IPluginContextCreator PluginContextCreator { get; }

    public PromptStageHandler(ILogger<PromptStageHandler> logger, IPluginLoader pluginLoader, IPluginContextCreator pluginContextCreator)
    {
        _logger = logger;
        PluginLoader = pluginLoader;
        PluginContextCreator = pluginContextCreator;
    }

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
        
        // finish prompt stage
        await FinalizeStageAsync(pipelineContext);
        
        _logger.LogInformation($"Completed Prompt Stage Processing: {pipelineContext.Pipeline.Name}");
    }

    private async Task HandleTemplateAsync(PipelineContext pipelineContext)
    {
        // Logic to handle prompt template
        var promptTemplateConfiguration = pipelineContext.Pipeline.Prompt?.Template;

        if (promptTemplateConfiguration == null)
        {
            _logger.LogInformation("Prompt Stage: No prompt template found, using default (package: floom/prompt/template/default)");
            // using default prompt template 'floom/prompt/template/default'
            promptTemplateConfiguration = new PluginConfiguration()
            {
                Package = "floom/prompt/template/default",
                Configuration = new Dictionary<string, object>()
            };
        }
        
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
                    ResultData = pluginResult.Data as PromptTemplateResult
                });
            }
        }

        if (pipelineContext.pipelineRequest.file != null)
        {
            var promptTemplateResultEvents = pipelineContext.GetEvents().OfType<PromptTemplateResultEvent>();
            promptTemplateResultEvents.ToList().ForEach(e => e.ResultData.File = FileUtils.ConvertIFormFileToByteArrayAsync(pipelineContext.pipelineRequest.file).Result );
        }
    }

    private async Task HandleContextAsync(PipelineContext pipelineContext)
    {
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
                            ResultData = pluginResult.Data as PromptContextResult,
                        });
                    }
                }
            }
        }
    }

    private async Task HandleOptimizationAsync(PipelineContext pipelineContext)
    {
        var promptOptimizationConfiguration = pipelineContext.Pipeline.Prompt?.Optimization;

        if (promptOptimizationConfiguration != null && promptOptimizationConfiguration.Any())
        {
            _logger.LogInformation("Prompt Stage: Handling prompt optimization.");
        }
    }

    private async Task HandleValidationAsync(PipelineContext pipelineContext)
    {
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
    
    private async Task FinalizeStageAsync(PipelineContext pipelineContext)
    {
        var floomRequest = new FloomRequest();
        // Logic to finalize prompt stage
        var templateResult = pipelineContext.GetEvents().OfType<PromptTemplateResultEvent>();
        
        floomRequest.Prompt = templateResult.FirstOrDefault()?.ResultData;
        
        var contextResult = pipelineContext.GetEvents().OfType<PromptContextResultEvent>();
        
        if(contextResult.Any())
        {
            floomRequest.Context = contextResult.FirstOrDefault()?.ResultData;
        }
        
        pipelineContext.AddEvent(new PromptStageResultEvent { ResultData = floomRequest });
    }
}