using System.Text.Json.Serialization;
using Floom.Repository;
using Floom.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Data.Entities
{
    //schema: v1
    //kind: Data
    //name: docs-data
    //type: file
    //path: /dev/test/documentation.pdf -> fileId
    //split: pages
    //embeddings: docs-embeddings
    //vectorStore: docs-vectorstore

    [BsonIgnoreExtraElements]
    public class DataEntity : DatabaseEntity
    {
        [BsonRepresentation(BsonType.String)] public DataType type { get; set; }
        public string assetId { get; set; }
        [BsonRepresentation(BsonType.String)] public SplitType split { get; set; }
        public string embeddings { get; set; }
        public string vectorStore { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter<DataType>))]
    public enum DataType
    {
        File = 1,
        API = 2,
        WebHook = 3
    }

    [JsonConverter(typeof(StringEnumConverter<SplitType>))]
    public enum SplitType
    {
        Pages = 1,
        Paragraphs = 2
    }
}