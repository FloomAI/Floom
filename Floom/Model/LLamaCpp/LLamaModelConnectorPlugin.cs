using Floom.Model.LLamaCpp;
using Floom.Plugin;

namespace Floom.Model.LLama;

[FloomPlugin("floom/model/connector/llamacpp")]
public class LLamaModelConnectorPlugin : ModelConnectorPluginBase<LLamaCppClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        // Initialize LLamaCppClient specific settings here, if any
    }
}