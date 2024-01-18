using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Models
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
    public class Response : ObjectEntity
    {
        public ResponseType type { get; set; }
        public string language { get; set; }
        public uint maxSentences { get; set; }
        public uint maxCharacters { get; set; }
        public double temperature { get; set; }
        public List<string> examples { get; set; }
        public bool referToData { get; set; }

        //image
        public string resolution { get; set; }
        public uint options { get; set; } = 1;
        public string format { get; set; }
        public double quality { get; set; }

        public static ResponseType ConvertToResponseType(string input)
        {
            switch (input.ToLower())
            {
                case "text":
                    return ResponseType.Text;
                case "image":
                    return ResponseType.Image;
                case "video":
                    return ResponseType.Video;
                case "audio":
                    return ResponseType.Audio;
                default:
                    throw new ArgumentException("Invalid input string", nameof(input));
            }
        }

        public static string ConvertFromResponseType(ResponseType responseType)
        {
            switch (responseType)
            {
                case ResponseType.Text:
                    return "text";
                case ResponseType.Image:
                    return "image";
                case ResponseType.Video:
                    return "video";
                case ResponseType.Audio:
                    return "audio";
                default:
                    throw new ArgumentException("Invalid response type", nameof(responseType));
            }
        }
    }

    public enum ResponseType
    {
        Text = 1,
        Image = 2,
        Video = 3,
        Audio = 4
    }

    

}