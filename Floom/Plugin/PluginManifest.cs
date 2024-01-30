namespace Floom.Plugin;

public class PluginManifest
{
    public string? Description { get; set; }
    public string? Package { get; set; }
    public string? Version { get; set; }
    public string? Runtime { get; set; }
    public IEnumerable<string> SupportedFloomVersions { get; set; }
    public IEnumerable<string> SupportedStages { get; set; }
    public IEnumerable<string> Events { get; set; }
    public Dictionary<string, PluginManifestParameter> Parameters { get; set; }
    public PluginManifestOwnerInfo Owner { get; set; }
    
    public class PluginManifestParameter
    {
        public string Type { get; set; }
        public string Description { get; set; }
        public object DefaultValue { get; set; }
    }

    public class PluginManifestOwnerInfo
    {
        public string Name { get; set; }
    }
    
    public class PluginManifestSupportedStage
    {
        public string Stage { get; set; }
        public List<string> Steps { get; set; }
    }
}