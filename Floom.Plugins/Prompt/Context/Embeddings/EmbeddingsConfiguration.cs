namespace Floom.Plugins.Prompt.Context.Embeddings;

public class EmbeddingsConfiguration
{
    public string? Model;
        
    public EmbeddingsConfiguration(string? embeddingsModel)
    {
        Model = embeddingsModel;
    }
}