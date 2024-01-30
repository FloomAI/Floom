using System.Text.Json.Serialization;
using Floom.Repository;
using Floom.Utils;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace Floom.Entities.Response
{
    //schema: v1
    //kind: Response
    //name: docs-response
    //type: text
    //language: English
    //maxSentences: 3
    //maxCharacters: 1500
    //temperature: 0.9

    [BsonIgnoreExtraElements]
    public class ResponseEntity : DatabaseEntity
    {
        [BsonRepresentation(BsonType.String)] public ResponseType type { get; set; }
        public string? language { get; set; }
        public uint maxSentences { get; set; }
        public uint maxCharacters { get; set; }
        public double temperature { get; set; }
        public List<string>? examples { get; set; }

        public bool referToData { get; set; }

        //
        // //image
        public string? resolution { get; set; }
        [BsonIgnoreIfNull] public uint? options { get; set; }
        public string? format { get; set; }
        [BsonIgnoreIfNull] public double? quality { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter<ResponseType>))]
    public enum ResponseType
    {
        Text = 1,
        Image = 2,
        Video = 3,
        Audio = 4
    }
}