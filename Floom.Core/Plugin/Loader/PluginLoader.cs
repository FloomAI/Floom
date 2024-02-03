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

    // public IFloomPlugin? LoadPlugin(string packageName)
    // {
    //     // Get all types in the current assembly
    //     var types = Assembly.GetExecutingAssembly().GetTypes();
    //
    //     // Find the type that has the FloomPlugin attribute with the matching package name
    //     var pluginType = types.FirstOrDefault(t => t.GetCustomAttributes<FloomPluginAttribute>().Any(a => a.PackageName == packageName));
    //
    //     if (pluginType == null)
    //     {
    //         _logger.LogError($"Plugin {packageName} not found");
    //         return null;
    //     }
    //
    //     return (IFloomPlugin)Activator.CreateInstance(pluginType);
    // } 
    
    public IFloomPlugin? LoadPlugin(string packageName)
    {
        // Specify the plugins directory, relative to the current domain's base directory
        var pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        var pluginAssemblies = Directory.GetFiles(pluginsDirectory, "*.dll").Select(Assembly.LoadFrom);

        Type? pluginType = null;

        // Search each assembly for the first type that matches the FloomPlugin attribute with the given package name
        foreach (var assembly in pluginAssemblies)
        {
            pluginType = assembly.GetTypes().FirstOrDefault(t =>
                t.GetCustomAttributes<FloomPluginAttribute>(false).Any(a => a.PackageName == packageName));

            // Break if we found a plugin
            if (pluginType != null)
            {
                break;
            }
        }

        if (pluginType == null)
        {
            _logger.LogError($"Plugin {packageName} not found");
            return null;
        }

        return (IFloomPlugin)Activator.CreateInstance(pluginType);
    }

}