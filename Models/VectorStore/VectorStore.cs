using Floom.Managers.VectorStores;
using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    //schema: v1
    //kind: VectorStore
    //name: docs-vectorstore
    //vendor: Pinecone
    //apiKey: 824jf285hg828gj2g951gh18

    [BsonIgnoreExtraElements]
    public class VectorStore : ObjectEntity
    {
        public string vendor { get; set; } //if private vdb, don't supply provider at all - only supply URL, port, creds, api protocol
        public string apiKey { get; set; } //probably all cloud
        public string environment { get; set; } //pinecone cloud
        public string endpoint { get; set; } //milvus
        public int port { get; set; } //milvus
        public string username { get; set; } //milvus
        public string password { get; set; } //milvus
    }
}