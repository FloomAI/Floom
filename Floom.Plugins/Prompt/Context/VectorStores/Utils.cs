using Floom.Pipeline;

namespace Floom.Plugins.Prompt.Context.VectorStores;

public static class Utils
{
    public static string GetCollectionName(PipelineContext pipelineContext)
    {
        var pipelineName = pipelineContext.PipelineName;
        var pipelineId = string.IsNullOrEmpty(pipelineContext.Pipeline.UserId)
            ? pipelineContext.Pipeline.Id
            : pipelineContext.Pipeline.UserId;
        return $"pipeline_{pipelineName}_uid_{pipelineId}".Replace("-", "_").ToLower();
    }
}