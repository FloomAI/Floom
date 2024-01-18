using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    [BsonIgnoreExtraElements]
    public class Pipeline : ObjectEntity
    {
        public string model { get; set; }
        public string prompt { get; set; }
        public string response { get; set; }
        public bool chatHistory { get; set; }
        public List<string> data { get; set; }
    }
}