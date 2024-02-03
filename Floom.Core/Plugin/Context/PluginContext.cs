using Floom.Plugin.Base;
using Floom.Plugin.Manifest;

namespace Floom.Plugin.Context;

public class PluginContext
{
    public string Package { get; set; }
    public PluginConfiguration Configuration { get; set; }
    public PluginManifest? Manifest { get; set; }
}

public interface IPluginContextCreator
{
    Task<PluginContext> Create(PluginConfiguration configuration);
}

public class PluginContextCreator : IPluginContextCreator
{
    private readonly IPluginManifestLoader _pluginManifestLoader;

    public PluginContextCreator(IPluginManifestLoader pluginManifestLoader)
    {
        _pluginManifestLoader = pluginManifestLoader;
    }
    
    public async Task<PluginContext> Create(PluginConfiguration configuration)
    {
        return new PluginContext
        {
            Package = configuration.Package,
            Configuration = configuration,
            Manifest = await _pluginManifestLoader.Load(configuration.Package)
        };
    }
}