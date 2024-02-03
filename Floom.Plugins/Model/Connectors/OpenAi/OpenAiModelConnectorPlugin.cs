using Floom.Model;
using Floom.Plugin.Base;

namespace Floom.Plugins.Model.Connectors.OpenAi;

[FloomPlugin("floom/model/connector/openai")]
public class OpenAiModelConnectorPlugin : ModelConnectorPluginBase<OpenAiClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        _client.ApiKey = settings.ApiKey;
    }
}