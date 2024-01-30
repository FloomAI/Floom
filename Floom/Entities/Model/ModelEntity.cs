using System.Text.Json.Serialization;
using Floom.Repository;
using Floom.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

[JsonConverter(typeof(StringEnumConverter<ModelVendor>))]
public enum ModelVendor
{
    OpenAI,
    Ollama,
    LLama
}

namespace Floom.Entities.Model
{
    //schema: v1
    //kind: Model
    //name: docs-model
    //type: text
    //vendor: OpenAI
    //model: davinci-003
    //apiKey: 824jf285hg828gj2g951gh18

    [BsonIgnoreExtraElements]
    public class ModelEntity : DatabaseEntity
    {
        [BsonRepresentation(BsonType.String)]
        public ModelVendor vendor { get; set; } //Don't provide if custom private LLM

        public string model { get; set; }
        public string apiKey { get; set; }
        public string uri { get; set; } //for custom private LLMs
    }
}