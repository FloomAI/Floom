using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Repository
{
    public class DatabaseEntity
    {
        public ObjectId Id { get; set; } = ObjectId.Empty;
        public DateTime createdAt { get; set; }
        [BsonIgnoreIfNull] public string name { get; set; }
        public string createdBy { get; set; } //id of the api-key
    }
}