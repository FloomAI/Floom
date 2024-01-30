using Floom.Logs;
using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using Microsoft.AspNetCore.Mvc;

namespace Floom.VectorStores.Engines
{
    public class Milvus : VectorStoreProvider
    {
        readonly ILogger _logger;

        public Milvus()
        {
            _logger = FloomLoggerFactory.CreateLogger(GetType());
        }

        private IMilvusClient GetMilvusClient()
        {
            if (!int.TryParse(ConnectionArgs.Port, out var port))
            {
                port = 19530; // Default value if parsing fails
            }

            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{ConnectionArgs.Endpoint}", port,
                ConnectionArgs.Username, ConnectionArgs.Password);

            return milvusClient;
        }
        
        public async Task<bool> IsExistsCollection(string collectionName)
        {
            _logger.LogInformation("IsExistsCollection");
            IMilvusClient milvusClient = GetMilvusClient();

            //Check if this collection exists
            var hasCollection = await milvusClient.HasCollectionAsync(collectionName);

            return hasCollection;
        }

        public async Task<bool> CreateCollection(string collectionName, uint dimension)
        {
            _logger.LogInformation("CreateCollection");
            IMilvusClient milvusClient = GetMilvusClient();

            await milvusClient.CreateCollectionAsync(
                collectionName,
                new[]
                {
                    FieldType.Create("object_id", MilvusDataType.Int64, isPrimaryKey: true, autoId: true), //row ID
                    FieldType.CreateVarchar("section_id", 30), //page//paragrpah//whatever
                    FieldType.CreateVarchar("text", 4000), //metadata text
                    FieldType.CreateFloatVector("text_vectors", dimension),
                }
            );

            return true;
        }

        public async Task<bool> CreateIndex(string collectionName)
        {
            _logger.LogInformation("CreateIndex");
            
            IMilvusClient milvusClient = GetMilvusClient();

            await milvusClient.CreateIndexAsync(
                collectionName,
                "text_vectors", //the vectors field
                "default",
                MilvusIndexType.IVF_FLAT, //Use MilvusIndexType.IVF_FLAT.
                //MilvusIndexType.AUTOINDEX,//Use MilvusIndexType.AUTOINDEX when you are using zilliz cloud.
                MilvusMetricType.L2,
                new Dictionary<string, string>
                {
                    { "nlist", "1024" }
                }
            );

            return true;
        }

        public async Task<bool> LoadToMemory(string collectionName)
        {
            _logger.LogInformation("LoadToMemory");
            IMilvusClient milvusClient = GetMilvusClient();

            await milvusClient.LoadCollectionAsync(collectionName);

            return true;
        }

        public async Task<bool> InsertVectors(string collectionName, List<SectionVectors> sectionVectors)
        {
            IMilvusClient milvusClient = GetMilvusClient();

            //Combine all section vectors
            List<List<float>> allSectionVectorsValues = sectionVectors.Select(s => s.values).ToList();
            List<string> allSectionVectorsIds = sectionVectors.Select(s => s.id).ToList();
            List<string> allSectionVectorsText = sectionVectors.Select(s => s.text).ToList();

            //string partitionName = "";//"novel";//Donnot Use partition name when you are connecting milvus hosted by zilliz cloud.

            MilvusMutationResult result = await milvusClient.InsertAsync(collectionName,
                new Field[]
                {
                    //Field.Create("object_id",bookIds),
                    Field.Create("section_id", allSectionVectorsIds),
                    Field.Create("text", allSectionVectorsText),
                    Field.CreateFloatVector("text_vectors", allSectionVectorsValues),
                }
            );

            return true;
        }

        public override async Task<List<VectorSearchResult>> Search(List<float> vectors, uint topResults = 5)
        {
            _logger.LogInformation("Search");
            
            IMilvusClient milvusClient = GetMilvusClient();

            List<string> search_output_fields = new() { "object_id" };

            List<List<float>> search_vectors = new()
            {
                vectors
            };

            var searchResults = await milvusClient.SearchAsync(
                MilvusSearchParameters.Create(CollectionName, "text_vectors", search_output_fields)
                    .WithVectors(search_vectors)
                    .WithOutputFields(new List<string>() { "text", "section_id" })
                    .WithConsistencyLevel(MilvusConsistencyLevel.Strong)
                    .WithMetricType(MilvusMetricType.L2)
                    .WithTopK(topK: (int)topResults)
                    .WithParameter("nprobe", "10")
                    .WithParameter("offset", "5"));

            //MilvusSea

            List<VectorSearchResult> results = new List<VectorSearchResult>();

            var fieldsDataText =
                searchResults.Results.FieldsData.Where(fd => fd.FieldName == "text").First() as Field<string>;
            var fieldsDataSectionId =
                searchResults.Results.FieldsData.Where(fd => fd.FieldName == "section_id").First() as Field<string>;

            for (int i = 0; i < searchResults.Results.Scores.Count; i++)
            {
                VectorSearchResult result = new VectorSearchResult()
                {
                    score = searchResults.Results.Scores[i],
                    text = fieldsDataText.Data[i],
                    id = fieldsDataSectionId.Data[i]
                };

                results.Add(result);
            }

            return results.OrderByDescending(r => r.score).ToList();
        }

        public async Task<bool> DeleteCollection(string collectionName)
        {
            _logger.LogInformation("DeleteCollection");
            
            IMilvusClient milvusClient = GetMilvusClient();

            await milvusClient.DropCollectionAsync(collectionName);

            return true;
        }

        public override async Task<IActionResult> HealthCheck()
        {
            _logger.LogInformation("HealthCheck");
            
            IMilvusClient milvusClient = GetMilvusClient();

            try
            {
                var result = await milvusClient.HealthAsync();

                if (result is { IsHealthy: true })
                {
                    _logger.LogInformation("Milvus Connection Healthy");
                    return new OkObjectResult(new { Message = $"Milvus Connection Healthy" });
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Milvus Connection Failed");
            }

            return new BadRequestObjectResult(new
                { Message = $"Milvus Connection Failed", ErrorCode = VectorStoreErrors.ConnectionFailed });
        }

        public override async Task Prepare()
        {
            _logger.LogInformation("Prepare");
            // //Index name must consist of lower case alphanumeric characters or '-', and must start and end with an alphanumeric character
            // string collectionName = $"fp_{dataDto.id.ToLower().Replace("-", "_")}"; //TODO: regex this shit in validator of dataset name

            try
            {
                //Check if index exists
                if (await IsExistsCollection(CollectionName))
                {
                    //Delete entire collection
                    await DeleteCollection(CollectionName);
                }

                await CreateCollection(CollectionName, 1536);

                await CreateIndex(CollectionName);

                await LoadToMemory(CollectionName);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Milvus Error in Prepare");
            }
        }

        public override async Task CreateAndInsertVectors(List<string> chunks, List<List<float>> embeddingsVectors)
        {
            var batchSize = 20;
            //Create vectors
            List<SectionVectors> vectors = new List<SectionVectors>();

            for (int page = 0; page < embeddingsVectors.Count; page++)
            {
                vectors.Add(new SectionVectors()
                {
                    id = $"page{page}",
                    values = embeddingsVectors[page],
                    text = chunks[page] //TODO: SHOULD TRIM IN Milvus to max varchar length
                });
            }

            //Push Vectors (By Batches)
            for (int i = 0; i < vectors.Count; i += batchSize)
            {
                // Get the current batch
                var batch = vectors.Skip(i).Take(batchSize).ToList();
                _logger.LogInformation($"CreateAndInsertVectors Inserting {batchSize} vectors, batch {i} of {vectors.Count}");
                // Push Vectors
                await InsertVectors(CollectionName, batch);
            }
        }
    }
}