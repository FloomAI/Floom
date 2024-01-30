using Floom.Plugin;

namespace Floom.Model;

public class ModelConnectorPluginConfig : FloomPluginConfigBase
{
    public string? ApiKey { get; private set; }
    public string? Model { get; private set; }

    public ModelConnectorPluginConfig(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        ApiKey = configuration.TryGetValue("apikey", out var apiKey) ? apiKey as string : string.Empty;
        Model = configuration.TryGetValue("model", out var model) ? model as string : string.Empty;
    }

}