using MongoDB.Bson;

namespace Floom.Plugin.Base;

public class PluginConfigurationEntity
{
    public string package { get; set; }
    public BsonDocument configuration { get; set; }

    public PluginConfigurationEntity()
    {
        configuration = new BsonDocument();
    }
}