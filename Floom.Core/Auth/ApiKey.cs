using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Auth;

public class ApiKeyEntity : DatabaseEntity
{
    [BsonElement("Key")] public string? Key { get; set; }
    public string UserId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public TimeSpan Validity { get; set; } = TimeSpan.FromDays(365); // TTL default to 1 year
}