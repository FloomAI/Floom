using Floom.Plugin.Base;
using Floom.Plugins.Prompt.Context.Embeddings;
using Floom.Plugins.Prompt.Context.VectorStores;

namespace Floom.Plugins.Prompt.Context.Retriever;

public class ContextRetrieverPluginConfigBase : FloomPluginConfigBase
{
    public List<string> AssetsIds { get; private set; }

    public ContextRetrieverPluginConfigBase(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        // Attempt to retrieve the value associated with "assetid"
        if (configuration.TryGetValue("assetid", out var assetId))
        {
            // Check if the value is a List<string>
            if (assetId is List<object> listObj)
            {
                AssetsIds = listObj.Cast<string>().ToList();
            }
            else if (assetId is string str)
            {
                AssetsIds = new List<string> { str };
            }
        }
    }
}