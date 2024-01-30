namespace Floom.Plugin;

public class PluginConfigurationEntity
{
    public string package { get; set; }
    public Dictionary<string, object> configuration { get; set; }

    public PluginConfigurationEntity()
    {
        configuration = new Dictionary<string, object>();
    }
}