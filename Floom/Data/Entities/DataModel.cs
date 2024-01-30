using Floom.Embeddings.Entities;
using Floom.Entities;
using Floom.Entities.Assets;
using Floom.VectorStores;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Data.Entities
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
    public class DataModel : BaseModel
    {
        public DataType type { get; set; }
        public AssetModel? Asset { get; set; }
        public SplitType split { get; set; }
        public EmbeddingsModel? Embeddings { get; set; }
        public VectorStoreModel? VectorStore { get; set; }
    }
}