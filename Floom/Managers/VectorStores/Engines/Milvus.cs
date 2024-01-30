using Google.Protobuf.Collections;
using IO.Milvus;
using IO.Milvus.Client;
using IO.Milvus.Client.gRPC;
using IO.Milvus.Client.REST;
using Pinecone;
using System.Collections;
using System.Collections.Generic;

namespace Floom.Managers.VectorStores.Engines
{
    public class Milvus : VectorStore
    {
        public string Endpoint = string.Empty;
        public UInt16? Port = 19530;
        public string Username = string.Empty;
        public string Password = string.Empty;

        public Milvus(string Endpoint, UInt16? Port = 19530, string Username = "", string Password = "")
        {
            this.Endpoint = Endpoint;
            this.Port = Port;
            this.Username = Username;
            this.Password = Password;
        }

        public async Task<bool> IsExistsCollection(string collectionName)
        {
            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{this.Endpoint}:19530/", (int)this.Port, this.Username, this.Password);

            //Check if this collection exists
            var hasCollection = await milvusClient.HasCollectionAsync(collectionName);

            return hasCollection;
        }

        public async Task<bool> CreateCollection(string collectionName, uint dimension)
        {
            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{this.Endpoint}", (int)this.Port, this.Username, this.Password);

            await milvusClient.CreateCollectionAsync(
                collectionName,
                new[] {
                    FieldType.Create("object_id", MilvusDataType.Int64, isPrimaryKey:true, autoId: true), //row ID
                    FieldType.CreateVarchar("section_id", 30), //page//paragrpah//whatever
                    FieldType.CreateVarchar("text", 4000), //metadata text
                    FieldType.CreateFloatVector("text_vectors", dimension),
                }
            );

            return true;
        }

        public async Task<bool> CreateIndex(string collectionName)
        {
            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{this.Endpoint}", (int)this.Port, this.Username, this.Password);

            await milvusClient.CreateIndexAsync(
                collectionName,
                "text_vectors", //the vectors field
                "default",
                MilvusIndexType.IVF_FLAT,//Use MilvusIndexType.IVF_FLAT.
                //MilvusIndexType.AUTOINDEX,//Use MilvusIndexType.AUTOINDEX when you are using zilliz cloud.
                MilvusMetricType.L2,
                new Dictionary<string, string> {
                    { "nlist", "1024" }
                }
            );

            return true;
        }

        public async Task<bool> LoadToMemory(string collectionName)
        {
            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{this.Endpoint}", (int)this.Port, this.Username, this.Password);

            await milvusClient.LoadCollectionAsync(collectionName);

            return true;
        }

        public async Task<bool> InsertVectors(string collectionName, List<SectionVectors> sectionVectors)
        {
            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{this.Endpoint}", (int)this.Port, this.Username, this.Password);

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

        public async Task<List<VectorSearchResult>> Search(string collectionName, List<float> vectors, uint topResults = 5)
        {
            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{this.Endpoint}", (int)this.Port, this.Username, this.Password);

            List<string> search_output_fields = new() { "object_id" };

            List<List<float>> search_vectors = new() {
                vectors
            };
            
            var searchResults = await milvusClient.SearchAsync(
                MilvusSearchParameters.Create(collectionName, "text_vectors", search_output_fields)
                    .WithVectors(search_vectors)
                    .WithOutputFields(new List<string>() { "text", "section_id" })
                    .WithConsistencyLevel(MilvusConsistencyLevel.Strong)
                    .WithMetricType(MilvusMetricType.L2)
                    .WithTopK(topK: (int)topResults)
                    .WithParameter("nprobe", "10")
                    .WithParameter("offset", "5"));

            //MilvusSea

            List<VectorSearchResult> results = new List<VectorSearchResult>();

            var fieldsDataText = searchResults.Results.FieldsData.Where(fd => fd.FieldName == "text").First() as Field<string>;
            var fieldsDataSectionId = searchResults.Results.FieldsData.Where(fd => fd.FieldName == "section_id").First() as Field<string>;

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
            IMilvusClient milvusClient = new MilvusGrpcClient($"http://{this.Endpoint}", (int)this.Port, this.Username, this.Password);

            await milvusClient.DropCollectionAsync(collectionName);

            return true;
        }
    }
}