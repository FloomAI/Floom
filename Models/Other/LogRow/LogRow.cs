using Floom.Managers.VectorStores;
using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    [BsonIgnoreExtraElements]
    public class LogRow : ObjectEntity
    {
        public LogType type { get; set; }
        public string message { get; set; }
        public string info { get; set; }
        public string url { get; set; }

        //Http
        //Host (device+net)
    }

    public enum LogType
    {
        Success = 0,
        Error = 1,
        Warning = 2,
        Information = 3
    }
}