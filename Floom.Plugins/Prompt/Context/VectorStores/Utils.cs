namespace Floom.Plugins.Prompt.Context.VectorStores;

public static class Utils
{
    public static string GetCollectionName(string dataId, string embeddingsModelName)
    {
        return $"fp_{dataId}_{embeddingsModelName}".Replace("-", "_").ToLower();
    }
}