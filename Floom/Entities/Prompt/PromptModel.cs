using MongoDB.Bson;

namespace Floom.Entities.Prompt;

public class PromptModel : BaseModel
{
    public PromptType type { get; set; }
    public string? system { get; set; }
    public string? user { get; set; }

    public static PromptModel FromEntity(PromptEntity promptEntity)
    {
        return new PromptModel
        {
            Id = promptEntity.Id == ObjectId.Empty ? null : promptEntity.Id.ToString(),
            type = promptEntity.type,
            system = promptEntity.system,
            user = promptEntity.user
        };
    }
}