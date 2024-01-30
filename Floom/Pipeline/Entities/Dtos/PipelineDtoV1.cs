using Floom.Entities;

namespace Floom.Pipeline.Entities.Dtos;

public class PipelineDtoV1 : MainDto
{
    public List<string>? models { get; set; }
    public string? prompt { get; set; }
    public string? response { get; set; }
    public bool chatHistory { get; set; }
    public List<string>? data { get; set; }

    public static PipelineDtoV1 FromEntity(OldPipelineEntity oldPipelineEntity)
    {
        return new PipelineDtoV1()
        {
            id = oldPipelineEntity.name,
            schema = oldPipelineEntity.schema,
            models = oldPipelineEntity.models,
            prompt = oldPipelineEntity.prompt,
            response = oldPipelineEntity.response,
            chatHistory = oldPipelineEntity.chatHistory,
            data = oldPipelineEntity.data
        };
    }
}