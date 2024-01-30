using Floom.Misc;
using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    [BsonIgnoreExtraElements]
    public class LogRowEntity : DatabaseEntity
    {
        public LogType type { get; set; }
        public string? message { get; set; }
        public string? info { get; set; }
        public string? url { get; set; }

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