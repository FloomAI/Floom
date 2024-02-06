using Floom.Plugin.Base;

namespace Floom.Plugins.Common.BadWordsFilter;

public class FloomBadWordsFilterConfig : FloomPluginConfigBase
{
    public List<string> Disallow { get; private set; } = new();

    public FloomBadWordsFilterConfig(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        // Try to retrieve the "disallow" entry as an object
        if (configuration.TryGetValue("disallow", out var disallowObj))
        {
            // Assuming disallowObj is a List<string>
            if (disallowObj is IEnumerable<object> genericList)
            {
                // Attempt to convert if the actual type is IEnumerable<object> but expected to contain strings
                Disallow = genericList.Cast<string>().ToList();
            }
        }
    }
}