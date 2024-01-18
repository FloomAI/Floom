using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

public class ApiKey
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }

    [BsonElement("Key")]
    public string Key { get; set; }
}
