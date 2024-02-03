using Floom.Plugin;
using Floom.Plugin.Base;
using Floom.Repository;

namespace Floom.Pipeline.Entities;

public class PipelineEntity: DatabaseEntity
{
    public string kind { get; set; }
    public IEnumerable<PluginConfigurationEntity>? model { get; set; }
    public PromptStageEntity? prompt { get; set; }
    public ResponseStageEntity? response { get; set; }
    public IEnumerable<PluginConfigurationEntity>? global { get; set; }
    
    public class PromptStageEntity
    {
        public PluginConfigurationEntity? template { get; set; }
        public IEnumerable<PluginConfigurationEntity>? context { get; set; }
        public IEnumerable<PluginConfigurationEntity>? optimization { get; set; }
        public IEnumerable<PluginConfigurationEntity>? validation { get; set; }
    }

    public class ResponseStageEntity
    {
        public IEnumerable<PluginConfigurationEntity>? format { get; set; }
        public IEnumerable<PluginConfigurationEntity>? validation { get; set; }
    }
}