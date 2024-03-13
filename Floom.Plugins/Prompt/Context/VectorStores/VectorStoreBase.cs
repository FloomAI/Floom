using Floom.Plugins.Prompt.Context.Embeddings;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Plugins.Prompt.Context.VectorStores
{
    public abstract class VectorStoreProvider
    {
        protected VectorStoreConfiguration ConnectionArgs;
        public string CollectionName { get; set; }

        public abstract Task<IActionResult> HealthCheck();

        public abstract Task Prepare(uint vectorDimension);

        public abstract Task CreateAndInsertVectors(List<string> chunks, List<List<float>> embeddings);

        public abstract Task<List<VectorSearchResult>> Search(List<float> vectors, uint topResults = 5);

        public void SetConnectionArgs(VectorStoreConfiguration connectionArgs)
        {
            ConnectionArgs = connectionArgs;
        }
    }

    public class VectorSearchResult
    {
        public string id = string.Empty;
        public double score;
        public Dictionary<string, object> metadata = new Dictionary<string, object>();
        public List<float> values = new List<float>();
        public string text = string.Empty;
    }

    public class SectionVectors
    {
        public string id { get; set; } = string.Empty;
        public List<float> values { get; set; } = new List<float>();
        public string text { get; set; } = string.Empty;
    }
}