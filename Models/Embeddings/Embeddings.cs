using Azure.AI.OpenAI;
using Floom.Managers.AIProviders.Engines;
using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    //schema: v1
    //kind: Embeddings
    //name: docs-embeddings
    //type: text
    //vendor: OpenAI
    //model: text-embedding-ada-002
    //apiKey: 824jf285hg828gj2g951gh18

    [BsonIgnoreExtraElements]
    public class Embeddings : ObjectEntity
    {
        public EmbeddingsType type { get; set; }
        public string vendor { get; set; } //if private ai model, don't supply provider at all - only supply URL, port, creds, api protocol
        public string model { get; set; }
        public string apiKey { get; set; }
        public string url { get; set; } //For private ai model
    }

    public enum EmbeddingsType
    {
        Text = 1,
        Image = 2
    }
}