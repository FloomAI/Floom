using Floom.Plugin;

namespace Floom.Pipeline.Entities;

public class Pipeline
{
    public string Name { get; set; }
    public string Kind { get; set; }
    public IEnumerable<PluginConfiguration>? Model { get; set; }
    public PromptStage? Prompt { get; set; }
    public ResponseStage? Response { get; set; }
    public IEnumerable<PluginConfiguration>? Global { get; set; }
    
    public class PromptStage
    {
        public PluginConfiguration? Template { get; set; }
        public IEnumerable<PluginConfiguration>? Context { get; set; }
        public IEnumerable<PluginConfiguration>? Optimization { get; set; }
        public IEnumerable<PluginConfiguration>? Validation { get; set; }
    }

    public class ResponseStage
    {
        public IEnumerable<PluginConfiguration>? Format { get; set; }
        public IEnumerable<PluginConfiguration>? Validation { get; set; }
    }
}