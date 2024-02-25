using Floom.Base;
using Floom.Repository;

namespace Floom.Audit;

[CollectionName("audit")]
public class AuditRowEntity : DatabaseEntity
{
    public AuditAction action { get; set; }
    public string objectType { get; set; }
    public string objectId { get; set; }
    public string objectName { get; set; }
    public string messageId { get; set; }
    public string chatId { get; set; }

    public Dictionary<string, object> attributes { get; set; }
}

public enum AuditAction
{
    Create = 1,
    Update = 2,
    Delete = 3,
    Get = 4,
    GetById = 5,
    Floom = 10
}