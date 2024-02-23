using Floom.Repository;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Options;

namespace Floom.Audit
{
    [BsonIgnoreExtraElements]
    //Non-technical transactions, quearible later
    public class AuditRowEntity : DatabaseEntity
    {
        public AuditAction action { get; set; }
        public string objectType { get; set; }
        public string objectId { get; set; }
        public string objectName { get; set; }
        public string messageId { get; set; }
        public string chatId { get; set; }

        [BsonDictionaryOptions(DictionaryRepresentation.ArrayOfDocuments)]
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
}