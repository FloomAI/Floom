using Floom.Repository;
using Floom.Utils;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Floom.Plugin;

/**
 * This class is responsible for loading the plugin manifests from the plugins.yml file
 */
public interface IPluginManifestLoader
{
    Task LoadAndUpdateManifestsAsync();
    Task<PluginManifest?> Load(string packageName);    
}

public class PluginManifestLoader : IPluginManifestLoader
{
    private readonly ILogger<PluginManifestLoader> _logger;
    private readonly IRepository<PluginManifestEntity> _pluginsRepository;
    private const string ManifestFilePath = "Plugin/plugins.yml";

    public PluginManifestLoader(ILogger<PluginManifestLoader> logger, IRepositoryFactory repositoryFactory)
    {
        _pluginsRepository = repositoryFactory.Create<PluginManifestEntity>("plugins");
        _logger = logger;
    }

    public async Task LoadAndUpdateManifestsAsync()
    {
        try
        {
            var yamlContent = await File.ReadAllTextAsync(ManifestFilePath);
        
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .WithTypeConverter(new PluginManifestYamlConverter())
                .Build();
            
            var pluginManifests = deserializer.Deserialize<List<PluginManifestEntity>>(yamlContent);
        
            foreach (var manifest in pluginManifests)
            {
                var existingManifest = await _pluginsRepository.Get(manifest.package, "package");
                if (existingManifest == null || !IsManifestSame(existingManifest, manifest))
                {
                    await UpdateManifestInDatabase(manifest);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError("LoadAndUpdateManifestsAsync Failed");
            throw;
        }
    }
    
    private bool IsManifestSame(PluginManifestEntity existingManifest, PluginManifestEntity newManifest)
    {
        // Implement comparison logic, possibly comparing serialized JSON strings
        // or specific properties if more appropriate
        var existingManifestJson = JsonConvert.SerializeObject(existingManifest);
        var newManifestJson = JsonConvert.SerializeObject(newManifest);
        return existingManifestJson == newManifestJson;
    }

    private async Task UpdateManifestInDatabase(PluginManifestEntity manifest)
    {
        await _pluginsRepository.UpsertEntity(manifest, manifest.package);
    }

    public async Task<PluginManifest?> Load(string packageName)
    {
        var manifestEntity = await _pluginsRepository.Get(packageName, "package");
        return manifestEntity?.ToModel();
    }
}