using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Auth;

public class ApiKeyEntity : DatabaseEntity
{
    [BsonElement("Key")] public string? key { get; set; }
    public string userId { get; set; }
    public TimeSpan validity { get; set; } = TimeSpan.FromDays(365); // TTL default to 1 year
}