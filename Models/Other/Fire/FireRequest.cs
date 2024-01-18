using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models.Other.Floom
{

    [BsonIgnoreExtraElements]
    public class FloomRequest
    {
        [BsonElement("name")]
        public string pipelineId { get; set; } //Pipeline ID
        public string chatId { get; set; } = ""; //Chat ID
        public string input { get; set; } = ""; //User Input
        public Dictionary<string, string> variables { get; set; } //Vars
        public DataTransferType dataTransfer { get; set; }
    }

    public enum DataTransferType
    {
        Base64 = 1,
        //Url = 2
    }
}
