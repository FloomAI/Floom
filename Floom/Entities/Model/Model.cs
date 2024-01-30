using MongoDB.Bson;

namespace Floom.Entities.Model;

public class Model : BaseModel
{
    public ModelVendor vendor { get; set; } //Don't provide if custom private LLM

    public string model { get; set; }
    public string apiKey { get; set; }
    public string uri { get; set; } //for custom private LLMs

    public static Model FromEntity(ModelEntity modelEntity)
    {
        return new Model
        {
            Id = modelEntity.Id == ObjectId.Empty ? null : modelEntity.Id.ToString(),
            vendor = modelEntity.vendor,
            model = modelEntity.model,
            apiKey = modelEntity.apiKey,
            uri = modelEntity.uri
        };
    }
}