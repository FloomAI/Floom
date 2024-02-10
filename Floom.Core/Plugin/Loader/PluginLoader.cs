using System.Reflection;
using Floom.Plugin.Base;

namespace Floom.Plugin.Loader;

public interface IPluginLoader
{
    IFloomPlugin? LoadPlugin(string packageName);
}

public class PluginLoader : IPluginLoader
{
    private readonly ILogger<PluginLoader> _logger;

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }
    
    public IFloomPlugin? LoadPlugin(string packageName)
    {
        // Use the FLOOM_DEPENDENCIES_PATH environment variable if set; otherwise, default to "DLLs" folder
        var floomDependenciesPath = Environment.GetEnvironmentVariable("FLOOM_DEPENDENCIES_PATH") ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DLLs");
        var pluginFilePath = Path.Combine(floomDependenciesPath, "Floom.Plugins.dll");

        Assembly pluginAssembly;
        try
        {
            pluginAssembly = Assembly.LoadFrom(pluginFilePath);
        }
        catch (FileNotFoundException)
        {
            _logger.LogError($"Floom.Plugins.dll not found in {pluginFilePath}");
            return null;
        }

        var pluginType = pluginAssembly.GetTypes().FirstOrDefault(t =>
            t.GetCustomAttributes<FloomPluginAttribute>(false).Any(a => a.PackageName == packageName));

        if (pluginType == null)
        {
            _logger.LogError($"Plugin {packageName} not found in Floom.Plugins.dll");
            return null;
        }

        return (IFloomPlugin)Activator.CreateInstance(pluginType);
    }
}