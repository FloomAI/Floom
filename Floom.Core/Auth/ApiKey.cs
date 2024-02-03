using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Auth;

public class ApiKeyEntity : DatabaseEntity
{
    [BsonElement("Key")] public string? Key { get; set; }
}