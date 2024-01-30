namespace Floom.Plugin;

public abstract class FloomPluginConfigBase
{
    protected FloomPluginConfigBase(IDictionary<string, object> configuration)
    {
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));
        Load(configuration);
    }

    protected abstract void Load(IDictionary<string, object> configuration);
}