namespace Floom.Plugins.Prompt.Context.VectorStores;

public static class Utils
{
    public static string GetCollectionName(string pipelineName, string userId)
    {
        return $"pipeline-{pipelineName}_user-{userId}".Replace("-", "_").ToLower();
    }
}