using Floom.Plugin.Base;
using Floom.Plugins.Prompt.Context.Embeddings;
using Floom.Plugins.Prompt.Context.VectorStores;

namespace Floom.Plugins.Prompt.Context.Retriever;

public class ContextRetrieverPluginConfigBase : FloomPluginConfigBase
{
    public string? AssetId { get; private set; }
    public VectorStoreConfiguration? VectorStore { get; set; }
    public EmbeddingsConfiguration? Embeddings { get; set; }

    public ContextRetrieverPluginConfigBase(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        AssetId = configuration.TryGetValue("assetid", out var assetId) ? assetId as string : string.Empty;
        VectorStore = configuration.TryGetValue("vectorstore", out var vectorstore) ?  new VectorStoreConfiguration(vectorstore) : null;
        Embeddings = configuration.TryGetValue("embeddings", out var embeddings) ? new EmbeddingsConfiguration(embeddings)  : null;
    }
}