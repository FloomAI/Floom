using Floom.Repository;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Plugin;

public class PluginManifestEntity : DatabaseEntity
{
    public string? description { get; set; }
    public string? package { get; set; }
    public string? version { get; set; }
    public string? runtime { get; set; }
    public IEnumerable<string> supportedFloomVersions { get; set; }
    public IEnumerable<string> supportedStages { get; set; }
    public IEnumerable<string> events { get; set; }
    public Dictionary<string, PluginManifestEntityParameter> parameters { get; set; }
    public PluginManifestEntityOwnerInfo owner { get; set; }
    
    public class PluginManifestEntityParameter
    {
        public string type { get; set; }
        public string description { get; set; }
        [BsonElement("default")]
        public Dictionary<object, object> defaultValue { get; set; }
    }

    public class PluginManifestEntityOwnerInfo
    {
        public string name { get; set; }
    }
    
    public class PluginManifestEntitySupportedStage
    {
        public string stage { get; set; }
        public List<string> steps { get; set; }
    }

    public PluginManifest? ToModel()
    {
        return new PluginManifest()
        {
            Description = description,
            Package = package,
            Version = version,
            Runtime = runtime,
            SupportedFloomVersions = supportedFloomVersions,
            SupportedStages = supportedStages,
            Events = events,
            Parameters = parameters.ToDictionary(kvp => kvp.Key, kvp => new PluginManifest.PluginManifestParameter()
            {
                Type = kvp.Value.type,
                Description = kvp.Value.description,
                DefaultValue = kvp.Value.defaultValue
            }),
            Owner = new PluginManifest.PluginManifestOwnerInfo()
            {
                Name = owner.name
            }
        };
    }
}
