using Floom.Base;
using Floom.Repository;

namespace Floom.Auth;

[CollectionName("users")]
public class UserEntity : DatabaseEntity
{
    public string type { get; set; }
    public bool validated { get; set; }
    public string emailAddress { get; set; } = string.Empty;
    public string username { get; set; } = string.Empty;
    public string nickname { get; set; } = string.Empty;
}
