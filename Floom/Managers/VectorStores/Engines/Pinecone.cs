using Pinecone;

namespace Floom.Managers.VectorStores.Engines
{
    public class Pinecone : VectorStore
    {
        public string ApiKey = string.Empty;
        public string Environment = string.Empty;

        public Pinecone(string ApiKey, string environment)
        {
            this.ApiKey = ApiKey;
            this.Environment = environment;
        }

        public async Task<bool> IsExistsIndex(string indexName)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);
            var indexes = await pinecone.ListIndexes();
            return indexes.Contains(indexName);
        }

        public async Task<bool> CreateIndex(string indexName, uint dimension, Metric metric, int replicas = 1, string podType = "starter")
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            await pinecone.CreateIndex(indexName, dimension, metric);

            //Only paid tiers in Pinecone allow configuration
            if (podType != "starter")
            {
                await pinecone.ConfigureIndex(indexName, replicas, podType);
            }

            return true;
        }

        public async Task<bool> InsertVector(string indexName, string id, List<float> values, MetadataMap metadata)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            using var index = await pinecone.GetIndex(indexName);

            var vectors = new[]
            {
                new Vector
                {
                    Id = id,
                    Values = values.ToArray(),
                    Metadata = metadata
                }
            };
            await index.Upsert(vectors);

            return true;
        }

        public async Task<bool> InsertVectors(string indexName, List<Vector> vectors)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.Upsert(vectors);

            return true;
        }


        public async Task<List<VectorSearchResult>> Search(string indexName, List<float> vector, MetadataMap? metadataFilter = null, uint topResults = 5)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            using var index = await pinecone.GetIndex(indexName);

            List<VectorSearchResult> results = new List<VectorSearchResult>();

            ScoredVector[]? scoredVectors = null;

            if (metadataFilter == null)
            {
                scoredVectors = await index.Query(vector.ToArray(), topK: topResults, includeMetadata: true);
            }
            else
            {
                scoredVectors = await index.Query(vector.ToArray(), topK: topResults, filter: metadataFilter, includeMetadata: true);
            }

            //Convert to VectorSearchResult
            foreach (var scoredVector in scoredVectors)
            {
                var vsr = new VectorSearchResult()
                {
                    score = scoredVector.Score,
                    id = scoredVector.Id,
                    values = scoredVector.Values.ToList(),
                    metadata = new Dictionary<string, object>()
                };

                //Iterate KVP
                foreach (var kvp in scoredVector.Metadata)
                {
                    vsr.metadata.Add(kvp.Key, kvp.Value.Inner);
                    if (kvp.Key == "text")
                    {
                        vsr.text = (string)kvp.Value.Inner;
                    }
                }

                results.Add(vsr);
            }

            return results;
        }

        public async Task<bool> DeleteVector(string indexName, string id)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.Delete(new[] { id });

            return true;
        }

        public async Task<bool> DeleteVector(string indexName, MetadataMap metadataFilter)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.Delete(metadataFilter);

            return true;
        }

        public async Task<bool> DeleteAllVectors(string indexName)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.DeleteAll();

            return true;
        }


        public async Task<bool> DeleteIndex(string indexName)
        {
            using var pinecone = new PineconeClient(this.ApiKey, this.Environment);

            await pinecone.DeleteIndex(indexName);

            return true;
        }

    }
}
