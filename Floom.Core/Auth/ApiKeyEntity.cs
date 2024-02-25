using Floom.Base;
using Floom.Repository;

namespace Floom.Auth;

[CollectionName("api-keys")]
public class ApiKeyEntity : DatabaseEntity
{ 
    public string? key { get; set; }
    public string userId { get; set; }
    public TimeSpan validity { get; set; } = TimeSpan.FromDays(365); // TTL default to 1 year
}