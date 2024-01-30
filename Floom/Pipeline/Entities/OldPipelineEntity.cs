using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Pipeline.Entities
{
    [BsonIgnoreExtraElements]
    public class OldPipelineEntity : DatabaseEntity
    {
        public string schema { get; set; } = "v1";
        public List<string>? models { get; set; }
        public string? prompt { get; set; }
        public string? response { get; set; }
        public bool chatHistory { get; set; }
        public List<string>? data { get; set; }
    }
}