using Floom.Base;
using Floom.Pipeline;
using Floom.Plugin.Context;

namespace Floom.Plugin.Base;

public class FloomPluginBase : IFloomPlugin
{
    protected ILogger _logger;

    public FloomPluginBase()
    {
        _logger = CreateLogger();
    }
    
    protected string? GetPackage()
    {
        try
        {
            var attribute = (FloomPluginAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(FloomPluginAttribute));
            return attribute?.PackageName;
        }
        catch (Exception e)
        {
            _logger.LogError("Error getting plugin package name");
        }

        return null;
    }
    
    private ILogger CreateLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        return loggerFactory.CreateLogger(this.GetType());
    }

    public virtual void Initialize(PluginContext configuration)
    {
        _logger.LogInformation($"Initializing plugin {GetType()}");
    }

    public virtual async Task<PluginResult> Execute(PluginContext pluginContext,
        PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing Plugin {pluginContext.Package}");

        return new PluginResult
        {
            Success = true
        };
    }

    public virtual Task HandleEvent(string EventName, PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Handling event {EventName} for plugin {pluginContext.Package}");

        return null;
    }

    public void Terminate()
    {
        _logger.LogInformation($"Terminating plugin {GetType().Name}");
    }
}