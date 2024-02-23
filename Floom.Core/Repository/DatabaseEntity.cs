using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Repository
{
    public class DatabaseEntity
    {
        public ObjectId Id { get; set; } = ObjectId.Empty;
        public DateTime createdAt { get; set; }
        [BsonIgnoreIfNull] public string name { get; set; }
        
        [BsonRepresentation(BsonType.Document)]
        public Dictionary<string, object> createdBy { get; set; }

        public DatabaseEntity()
        {
            // Initialize with an empty dictionary to avoid null reference exceptions
            createdBy = new Dictionary<string, object>();
        }
        
        // Method to add a key-value pair to the createdBy dictionary
        public void AddCreatedByApiKey(object value)
        {
            var key = "apiKey";
            createdBy[key] = value;
        }
        
        public void AddCreatedByOwner(object value)
        {
            var key = "owner";
            createdBy[key] = value;
        }
    }
}