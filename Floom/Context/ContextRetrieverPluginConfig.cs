using Floom.Context.Embeddings;
using Floom.Plugin;
using Floom.VectorStores;

namespace Floom.Context;

public class ContextRetrieverPluginConfig : FloomPluginConfigBase
{
    public string? AssetId { get; private set; }
    public VectorStoreConfiguration? VectorStore { get; set; }
    public EmbeddingsConfiguration? Embeddings { get; set; }

    public ContextRetrieverPluginConfig(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        AssetId = configuration.TryGetValue("assetid", out var assetId) ? assetId as string : string.Empty;
        VectorStore = configuration.TryGetValue("vectorstore", out var vectorstore) ?  new VectorStoreConfiguration(vectorstore) : null;
        Embeddings = configuration.TryGetValue("embeddings", out var embeddings) ? new EmbeddingsConfiguration(embeddings)  : null;
    }
}