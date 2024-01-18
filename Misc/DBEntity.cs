using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Misc
{
    public class DBEntity
    {
        public ObjectId Id { get; set; }
        public DateTime createdAt { get; set; }
    }

    public class ObjectEntity : DBEntity
    {
        [BsonIgnoreIfNull]
        public string name { get; set; }
        public string createdBy { get; set; } //id of the api-key

    }
}
