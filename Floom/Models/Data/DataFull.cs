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
    public class DataFull : ObjectEntity
    {
        public DataType type { get; set; }
        public File file { get; set; }
        public SplitType split { get; set; }
        public Embeddings embeddings { get; set; }
        public VectorStore vectorStore { get; set; } 
    }
}