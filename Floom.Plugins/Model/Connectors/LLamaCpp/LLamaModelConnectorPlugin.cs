using Floom.Model;
using Floom.Plugin.Base;

namespace Floom.Plugins.Model.Connectors.LLamaCpp;

[FloomPlugin("floom/model/connector/llamacpp")]
public class LLamaModelConnectorPlugin : ModelConnectorPluginBase<LLamaCppClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        // Initialize LLamaCppClient specific settings here, if any
    }
}