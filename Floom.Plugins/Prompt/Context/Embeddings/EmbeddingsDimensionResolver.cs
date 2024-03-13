using Floom.Plugins.Prompt.Context.Embeddings.Gemini;
using Floom.Plugins.Prompt.Context.Embeddings.OpenAi;

namespace Floom.Plugins.Prompt.Context.Embeddings;

public class EmbeddingsDimensionResolver
{
    public static uint GetDimension(EmbeddingsProvider embeddingsProvider)
    {
        return embeddingsProvider switch
        {
            OpenAiEmbeddings => 1536,
            GeminiEmbeddings => 768,
            _ => 1536, // Default case, could adjust based on actual default needs
        };
    }

}