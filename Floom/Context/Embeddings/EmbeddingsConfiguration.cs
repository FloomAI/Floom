namespace Floom.Context.Embeddings;

public class EmbeddingsConfiguration
{
    public string? Vendor;
    public string? Model;
        
    public EmbeddingsConfiguration(object configuration)
    {
        if (configuration is not IDictionary<object, object> dict) return;
        Vendor = dict.TryGetValue("vendor", out var vendor) ? vendor as string : string.Empty;
        Model = dict.TryGetValue("model", out var model) ? model as string : string.Empty;
    }
}