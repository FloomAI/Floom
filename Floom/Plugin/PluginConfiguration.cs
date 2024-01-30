namespace Floom.Plugin;

public class PluginConfiguration
{
    public string Package { get; set; }
    public Dictionary<string, object> Configuration { get; set; }

    public PluginConfiguration()
    {
        Configuration = new Dictionary<string, object>();
    }
}