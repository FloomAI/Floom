using Floom.Plugins.Prompt.Context.VectorStores.Engines;

namespace Floom.Plugins.Prompt.Context.VectorStores;

public static class VectorStoresFactory
{
    public static VectorStoreProvider Create(VectorStoreConfiguration vectorStoreConfiguration)
    {
        switch (vectorStoreConfiguration.Vendor.ToLower())
        {
            case "pinecone":
            {
                var provider = new Plugins.Prompt.Context.VectorStores.Engines.Pinecone();
                return provider;
            }
            case "milvus":
            {
                var provider = new Milvus();
                provider.SetConnectionArgs(vectorStoreConfiguration);
                return provider;
            }
            case "postgres":
            {
                var provider = new PgVector();
                provider.SetConnectionArgs(vectorStoreConfiguration);
                return provider;
            }
        }

        throw new Exception("No Vector Store Provider found for vendor: " + vectorStoreConfiguration.Vendor);
    }
}