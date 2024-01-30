using System.Text.Json.Serialization;
using Floom.Repository;
using Floom.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

[JsonConverter(typeof(StringEnumConverter<PromptType>))]
public enum PromptType
{
    Text = 1
}

namespace Floom.Entities.Prompt
{
    //schema: v1
    //kind: Prompt
    //name: docs-prompt
    //type: text
    //system: "You are an assitant that speaks like Shakespear" #optional
    //user: "{input}"

    [BsonIgnoreExtraElements]
    public class PromptEntity : DatabaseEntity
    {
        [BsonRepresentation(BsonType.String)] public PromptType type { get; set; }
        public string? system { get; set; }
        public string? user { get; set; }
    }
}