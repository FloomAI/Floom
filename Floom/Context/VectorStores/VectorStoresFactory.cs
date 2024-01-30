using Floom.VectorStores;
using Floom.VectorStores.Engines;

namespace Floom.Context.VectorStores;

public static class VectorStoresFactory
{
    public static VectorStoreProvider Create(VectorStoreConfiguration vectorStoreConfiguration)
    {
        switch (vectorStoreConfiguration.Vendor.ToLower())
        {
            case "pinecone":
            {
                var provider = new Floom.VectorStores.Engines.Pinecone();
                return provider;
            }
            case "milvus":
            {
                var provider = new Milvus();
                provider.SetConnectionArgs(vectorStoreConfiguration);
                return provider;
            }
        }

        throw new Exception("No Vector Store Provider found for vendor: " + vectorStoreConfiguration.Vendor);
    }
}