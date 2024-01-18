using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
{
    //schema: v1
    //kind: Prompt
    //name: docs-prompt
    //type: text
    //system: "You are an assitant that speaks like Shakespear" #optional
    //user: "{input}"

    [BsonIgnoreExtraElements]
    public class Prompt : ObjectEntity
    {
        public PromptType type { get; set; }
        public string system { get; set; }
        public string user { get; set; }
    }

    public enum PromptType
    {
        Text = 1
    }
}