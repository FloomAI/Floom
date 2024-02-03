using Floom.Pipeline;
using Floom.Plugin.Context;

namespace Floom.Plugin.Base;

public class PluginResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? ResultData { get; set; }
}

public interface IFloomPlugin
{
    void Initialize(PluginContext configuration);
    Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext);
    Task HandleEvent(string EventName, PluginContext pluginContext, PipelineContext pipelineContext);
    void Terminate();
}