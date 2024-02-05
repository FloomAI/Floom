using Floom.Plugin.Base;

namespace Floom.Plugins.Model.Connectors;

public class ModelConnectorPluginConfig : FloomPluginConfigBase
{
    public string? ApiKey { get; private set; }
    public string? Model { get; private set; }
    public string? Voice { get; set; }

    public ModelConnectorPluginConfig(IDictionary<string, object> configuration) : base(configuration) { }

    protected override void Load(IDictionary<string, object> configuration)
    {
        ApiKey = configuration.TryGetValue("apikey", out var apiKey) ? apiKey as string : string.Empty;
        Model = configuration.TryGetValue("model", out var model) ? model as string : string.Empty;
        Voice = configuration.TryGetValue("voice", out var voice) ? voice as string : string.Empty;
    }

}