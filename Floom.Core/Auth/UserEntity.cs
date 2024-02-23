using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Auth;

[BsonIgnoreExtraElements]
public class UserEntity : DatabaseEntity
{
    public string type { get; set; }
    public bool validated { get; set; }
    public string emailAddress { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
}
