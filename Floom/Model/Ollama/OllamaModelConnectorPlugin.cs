using Floom.Plugin;

namespace Floom.Model.Ollama;

[FloomPlugin("floom/model/connector/ollama")]
public class OllamaModelConnectorPlugin : ModelConnectorPluginBase<OllamaClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        // Initialize OllamaClient specific settings here, if any
    }
}