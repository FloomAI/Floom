using Floom.Managers.AIProviders.Engines;
using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    //schema: v1
    //kind: Model
    //name: docs-model
    //type: text
    //vendor: OpenAI
    //model: davinci-003
    //apiKey: 824jf285hg828gj2g951gh18

    [BsonIgnoreExtraElements]
    public class Model : ObjectEntity
    {
        public string vendor { get; set; } //Don't provide if custom private LLM
        public string model { get; set; }
        public string apiKey { get; set; }
        public string uri { get; set; } //for custom private LLMs
    }
}