using Floom.Plugins.Prompt.Context.Embeddings.Ollama;
using Floom.Plugins.Prompt.Context.Embeddings.OpenAi;

namespace Floom.Plugins.Prompt.Context.Embeddings;

public static class EmbeddingsFactory
{
    public static EmbeddingsProvider Create(string vendor, string? apiKey = null, string? model = null)
    {
        switch (vendor.ToLower())
        {
            case "openai":
            {
                var provider = new OpenAiEmbeddings();

                if (provider == null)
                    throw new Exception("OpenAiEmbeddings not found in DI container");

                if (apiKey != null)
                {
                    provider.SetApiKey(apiKey);
                }

                if (model != null)
                {
                    provider.Model = model;
                }

                return provider;
            }
            case "ollama":
            {
                var provider = new OllamaEmbeddings();
                
                if (provider == null)
                    throw new Exception("OllamaEmbeddings not found in DI container");

                if (model != null)
                {
                    provider.Model = model;
                }
                
                return provider;
            }
        }

        throw new Exception("No Embeddings Provider found for vendor: " + vendor);
    }
}