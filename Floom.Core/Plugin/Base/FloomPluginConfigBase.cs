namespace Floom.Plugin.Base;

public abstract class FloomPluginConfigBase
{
    public FloomPluginConfigBase(IDictionary<string, object> configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        Load(configuration);
    }
    
    protected abstract void Load(IDictionary<string, object> configuration);
}