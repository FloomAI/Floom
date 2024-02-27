using Floom.Pipeline.Entities;
using Floom.Plugin;
using Floom.Plugin.Base;

namespace Floom.Pipeline;

public class PipelineHelper
{
    public static IEnumerable<PluginConfiguration> GetPipelinePluginsConfigurations(PipelineEntity pipelineEntity)
    {
        var plugins = new List<PluginConfiguration>();

        if (pipelineEntity.model != null)
        {
            plugins.AddRange(pipelineEntity.model.Select(m => m.ToModel()));
        }

        if (pipelineEntity.prompt != null)
        {
            if (pipelineEntity.prompt.template != null)
            {
                plugins.Add(pipelineEntity.prompt.template.ToModel());
            }

            if (pipelineEntity.prompt.context != null)
            {
                plugins.AddRange(pipelineEntity.prompt.context.Select(c => c.ToModel()));
            }

            if (pipelineEntity.prompt.optimization != null)
            {
                plugins.AddRange(pipelineEntity.prompt.optimization.Select(o => o.ToModel()));
            }

            if (pipelineEntity.prompt.validation != null)
            {
                plugins.AddRange(pipelineEntity.prompt.validation.Select(v => v.ToModel()));
            }
        }

        if (pipelineEntity.response != null)
        {
            if (pipelineEntity.response.format != null)
            {
                plugins.AddRange(pipelineEntity.response.format.Select(f => f.ToModel()));
            }

            if (pipelineEntity.response.validation != null)
            {
                plugins.AddRange(pipelineEntity.response.validation.Select(v => v.ToModel()));
            }
        }

        if (pipelineEntity.global != null)
        {
            plugins.AddRange(pipelineEntity.global.Select(g => g.ToModel()));
        }

        return plugins;
    }

    public static PluginConfiguration? GetPluginConfigurationInPipeline(string pluginPackage, PipelineEntity pipelineEntity)
    {
        var pluginConfig = GetPipelinePluginsConfigurations(pipelineEntity).FirstOrDefault(p => p.Package == pluginPackage);
    
        if (pluginConfig is { Configuration: not null })
        {
            pluginConfig.Configuration = LowerCaseKeys(pluginConfig.Configuration);
        }
    
        return pluginConfig;
    }
    
    private static Dictionary<string, object> LowerCaseKeys(Dictionary<string, object> originalConfiguration)
    {
        var lowerCaseConfiguration = new Dictionary<string, object>();

        foreach (var kvp in originalConfiguration)
        {
            lowerCaseConfiguration[kvp.Key.ToLower()] = kvp.Value;
        }

        return lowerCaseConfiguration;
    }

}