namespace Floom.Managers.VectorStores
{
    public abstract class VectorStore
    {
        //public abstract Task<bool> CreateIndex(string indexName);

        //public abstract Task<bool> IsExistsIndex(string indexName);

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
