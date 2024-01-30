using System.Text.Json.Serialization;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Pipeline.Entities
{
    [BsonIgnoreExtraElements]
    public class FloomResponse
    {
        public string messageId { get; set; } = "";
        public string chatId { get; set; } = "";
        public List<ResponseValue> values { get; set; } = new List<ResponseValue>();
        public long processingTime { get; set; }
        public FloomResponseTokenUsage tokenUsage { get; set; }
    }

    public class ResponseValue
    {
        public DataType type { get; set; } = DataType.String;
        public string format { get; set; } = "";
        public string value { get; set; } = ""; //String value
        public string b64 { get; set; } = "";
        public string url { get; set; } = "";

        [JsonIgnore] public byte[]? valueRaw { get; set; } = null;
    }

    public enum DataType
    {
        String = 1,
        Image = 2,
        Video = 3,
        Audio = 4
    }

    [BsonIgnoreExtraElements]
    public class FloomResponseTokenUsage
    {
        public int processingTokens { get; set; }
        public int promptTokens { get; set; }
        public int totalTokens { get; set; }
    }
}