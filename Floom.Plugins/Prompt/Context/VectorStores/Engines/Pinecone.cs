using Floom.Logs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Pinecone;

namespace Floom.Plugins.Prompt.Context.VectorStores.Engines
{
    public class Pinecone : VectorStoreProvider
    {
        // public string ApiKey = string.Empty;
        // public string Environment = string.Empty;

        readonly ILogger _logger;

        public Pinecone()
        {
            _logger = FloomLoggerFactory.CreateLogger(GetType());
        }

        // public Pinecone(string ApiKey, string environment)
        // {
        //     this.ApiKey = ApiKey;
        //     this.Environment = environment;
        // }

        public async Task<bool> IsExistsIndex(string indexName)
        {
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);
            var indexes = await pinecone.ListIndexes();
            return indexes.Contains(indexName);
        }

        public async Task<bool> CreateIndex(string indexName, uint dimension, Metric metric, int replicas = 1,
            string podType = "starter")
        {
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

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
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

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
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.Upsert(vectors);

            return true;
        }

        public override async Task<List<VectorSearchResult>> Search(List<float> vectors, uint topResults = 5)
        {
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

            using var index = await pinecone.GetIndex(CollectionName);

            List<VectorSearchResult> results = new List<VectorSearchResult>();

            ScoredVector[]? scoredVectors = null;

            // if (metadataFilter == null)
            {
                scoredVectors = await index.Query(vectors.ToArray(), topK: topResults, includeMetadata: true);
            }
            // else
            // {
            // scoredVectors = await index.Query(vector.ToArray(), topK: topResults, filter: metadataFilter,
            // includeMetadata: true);
            // }

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
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.Delete(new[] { id });

            return true;
        }

        public async Task<bool> DeleteVector(string indexName, MetadataMap metadataFilter)
        {
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.Delete(metadataFilter);

            return true;
        }

        public async Task<bool> DeleteAllVectors(string indexName)
        {
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

            using var index = await pinecone.GetIndex(indexName);

            await index.DeleteAll();

            return true;
        }


        public async Task<bool> DeleteIndex(string indexName)
        {
            using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);

            await pinecone.DeleteIndex(indexName);

            return true;
        }

        public override async Task<IActionResult> HealthCheck()
        {
            try
            {
                using var pinecone = new PineconeClient(ConnectionArgs.ApiKey, ConnectionArgs.Environment);
                return new OkObjectResult(new { Message = $"Pinecone Connection Healthy" });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Milvus Connection Failed");

                return new BadRequestObjectResult(new
                    { Message = $"Pinecone Connection Failed", ErrorCode = VectorStoreErrors.ConnectionFailed });
            }
        }

        public override async Task Prepare()
        {
            try
            {
                //Check if index exists
                if (await IsExistsIndex(CollectionName))
                {
                    //Delete all vectors in it (Reset it)
                    await DeleteAllVectors(CollectionName);
                }
                else //Not exists, create it
                {
                    await CreateIndex(CollectionName, 1536, Metric.Cosine);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Pinecone Prepare Failed");
            }
        }

        public override async Task CreateAndInsertVectors(List<string> chunks, List<List<float>> embeddings)
        {
            try
            {
                int batchSize = 20;

                //Create vectors
                List<Vector> vectors = new List<Vector>();

                for (int page = 0; page < embeddings.Count; page++)
                {
                    vectors.Add(new Vector()
                    {
                        Id = $"page{page}",
                        Values = embeddings[page].ToArray(),
                        Metadata = new MetadataMap()
                        {
                            ["text"] = chunks[page],
                            ["page"] = page
                        },
                    });
                }

                //Push Vectors (By Batches)
                for (int i = 0; i < vectors.Count; i += batchSize)
                {
                    // Get the current batch
                    var batch = vectors.Skip(i).Take(batchSize).ToList();

                    // Push Vectors
                    await InsertVectors(CollectionName, batch);
                }
            }
            catch (Exception e)
            {
                await DeleteAllVectors(CollectionName);
                _logger.LogError(e, "Pinecone CreateAndInsertVectors Failed");
            }
        }
    }
}