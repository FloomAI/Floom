using Floom.Plugin;

namespace Floom.Pipeline;

public interface IStageHandler
{
    IPluginContextCreator PluginContextCreator { get; }
    IPluginLoader PluginLoader { get; }

    Task ExecuteAsync(PipelineContext pipelineContext);
}