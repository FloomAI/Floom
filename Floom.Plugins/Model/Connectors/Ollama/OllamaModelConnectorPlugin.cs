using Floom.Model;
using Floom.Plugin.Base;

namespace Floom.Plugins.Model.Connectors.Ollama;

[FloomPlugin("floom/model/connector/ollama")]
public class OllamaModelConnectorPlugin : ModelConnectorPluginBase<OllamaClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        // Initialize OllamaClient specific settings here, if any
    }
}