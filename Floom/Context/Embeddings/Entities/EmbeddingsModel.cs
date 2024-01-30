using Floom.Entities;
using Floom.Entities.Embeddings;
using MongoDB.Bson;

namespace Floom.Embeddings.Entities;

public class EmbeddingsModel : BaseModel
{
    public EmbeddingsType type { get; set; }
    public ModelVendor vendor { get; set; }
    public string model { get; set; }
    public string apiKey { get; set; }
    public string url { get; set; }


    public static EmbeddingsModel FromEntity(EmbeddingsEntity embeddingsEntity)
    {
        return new EmbeddingsModel
        {
            Id = embeddingsEntity.Id == ObjectId.Empty ? null : embeddingsEntity.Id.ToString(),
            type = embeddingsEntity.type,
            vendor = embeddingsEntity.vendor,
            model = embeddingsEntity.model,
            apiKey = embeddingsEntity.apiKey,
            url = embeddingsEntity.url
        };
    }

    public EmbeddingsEntity ToEntity()
    {
        return new EmbeddingsEntity
        {
            Id = Id != null ? ObjectId.Parse(Id) : ObjectId.Empty,
            name = name,
            type = type,
            vendor = vendor,
            model = model,
            apiKey = apiKey,
            url = url
        };
    }
}