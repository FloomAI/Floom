using System.Text.Json.Serialization;
using Floom.Repository;
using Floom.Utils;
using Floom.VectorStores;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


public static class VectorStoreVendor
{
    [JsonConverter(typeof(StringEnumConverter<Enum>))]
    public enum Enum
    {
        Milvus,
        Pinecone
    }

    public static Enum FromString(string value)
    {
        return Enum.TryParse<Enum>(value, true, out var result)
            ? result
            : throw new ArgumentException($"Invalid VectorStoreVendor: {value}");
    }

    public static string ToString(Enum value)
    {
        return value.ToString();
    }
}

namespace Floom.Entities.VectorStore
{
    //schema: v1
    //kind: VectorStore
    //name: docs-vectorstore
    //vendor: Pinecone
    //apiKey: 824jf285hg828gj2g951gh18

    [BsonIgnoreExtraElements]
    public class VectorStoreEntity : DatabaseEntity
    {
        [BsonRepresentation(BsonType.String)] public VectorStoreVendor.Enum vendor { get; set; }
        public string? apiKey { get; set; } //probably all cloud
        public string? environment { get; set; } //pinecone cloud
        public string? endpoint { get; set; } //milvus
        public int port { get; set; } //milvus
        public string? username { get; set; } //milvus
        public string? password { get; set; } //milvus
    }
}