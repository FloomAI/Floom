using Floom.Repository;
using Floom.Utils;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using MongoDB.Bson;


[JsonConverter(typeof(StringEnumConverter<EmbeddingsType>))]
public enum EmbeddingsType
{
    Text = 1,
    Image = 2
}

namespace Floom.Entities.Embeddings
{
    //schema: v1
    //kind: Embeddings
    //name: docs-embeddings
    //type: text
    //vendor: OpenAI
    //model: text-embedding-ada-002
    //apiKey: 824jf285hg828gj2g951gh18

    [BsonIgnoreExtraElements]
    public class EmbeddingsEntity : DatabaseEntity
    {
        [BsonRepresentation(BsonType.String)] public EmbeddingsType type { get; set; }
        [BsonRepresentation(BsonType.String)] public ModelVendor vendor { get; set; }
        public string model { get; set; }
        public string apiKey { get; set; }
        public string url { get; set; }
    }
}