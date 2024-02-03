using Floom.Plugin;
using Floom.Plugin.Context;
using Floom.Plugin.Loader;

namespace Floom.Pipeline;

public interface IStageHandler
{
    IPluginContextCreator PluginContextCreator { get; }
    IPluginLoader PluginLoader { get; }

    Task ExecuteAsync(PipelineContext pipelineContext);
}