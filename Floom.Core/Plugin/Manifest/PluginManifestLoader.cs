using System.Reflection;
using Floom.Repository;
using Floom.Utils;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Floom.Plugin.Manifest;

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

    public PluginManifestLoader(ILogger<PluginManifestLoader> logger, IRepositoryFactory repositoryFactory)
    {
        _pluginsRepository = repositoryFactory.Create<PluginManifestEntity>("plugins");
        _logger = logger;
    }

    private static string GetManifestFilePath()
    {
        var assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var manifestFilePath = Path.Combine(assemblyLocation, "Plugin", "plugins.yml");
        return manifestFilePath;
    }

    public async Task LoadAndUpdateManifestsAsync()
    {
        try
        {
            var yamlContent = await File.ReadAllTextAsync(GetManifestFilePath());
        
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
            _logger.LogError(e, "LoadAndUpdateManifestsAsync Failed11");
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
        manifest.AddCreatedByOwner("floom-manifest-loader");
        await _pluginsRepository.UpsertEntity(manifest, manifest.package);
    }

    public async Task<PluginManifest?> Load(string packageName)
    {
        var manifestEntity = await _pluginsRepository.Get(packageName, "package");
        return manifestEntity?.ToModel();
    }
}