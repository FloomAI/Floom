using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    [BsonIgnoreExtraElements]
    public class PipelineFull : ObjectEntity
    {
        public Model model { get; set; }
        public Prompt prompt { get; set; }
        public Response response { get; set; }
        public bool chatHistory { get; set; }
        public List<DataFull> data { get; set; }
    }
}