using Floom.LLMs.LLama;
using Floom.LLMs.Ollama;
using Floom.LLMs.OpenAi;

namespace Floom.LLMs;

public interface ILLMFactory
{
    public LLMProvider Create(ModelVendor vendor);
}

public class LLMFactory : ILLMFactory
{
    private readonly IServiceProvider _serviceProvider;

    public LLMFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public LLMProvider Create(ModelVendor vendor)
    {
        switch (vendor)
        {
            case ModelVendor.OpenAI:
            {
                var provider = _serviceProvider.GetService(typeof(OpenAiLLM));

                if (provider != null)
                {
                    return (LLMProvider)provider;
                }

                throw new Exception("OpenAiLLM not found in DI container");
            }
            case ModelVendor.Ollama:
            {
                var provider = _serviceProvider.GetService(typeof(OllamaLLM));
                if (provider != null)
                {
                    return (LLMProvider)provider;
                }

                throw new Exception("OllamaLLM not found in DI container");
            }
            case ModelVendor.LLama:
            {
                var provider = _serviceProvider.GetService(typeof(LLamaLLM));
                if (provider != null)
                {
                    return (LLMProvider)provider;
                }

                throw new Exception("LLamaLLM not found in DI container");
            }

        }

        throw new Exception("No LLM Provider found for vendor: " + vendor);
    }

    public static ILLMFactory GetFactory(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<ILLMFactory>()!;
    }
}