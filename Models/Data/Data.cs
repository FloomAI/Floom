using Azure.AI.OpenAI;
using Floom.Misc;
using MongoDB.Bson.Serialization.Attributes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Floom.Models
{
    //schema: v1
    //kind: Data
    //name: docs-data
    //type: file
    //path: /dev/test/documentation.pdf -> fileId
    //split: pages
    //embeddings: docs-embeddings
    //vectorStore: docs-vectorstore

    [BsonIgnoreExtraElements]
    public class Data : ObjectEntity
    {
        public DataType type { get; set; }
        public string fileId { get; set; }
        public SplitType split { get; set; }
        public string embeddings { get; set; }
        public string vectorStore { get; set; } 
    }

    public enum DataType
    {
        File = 1,
        API = 2,
        WebHook = 3
    }

    public enum SplitType
    {
        Pages = 1,
        Paragraphs = 2
    }
}