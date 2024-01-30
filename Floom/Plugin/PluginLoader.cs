using System.Reflection;
using Floom.Pipeline;

namespace Floom.Plugin;

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
        // Get all types in the current assembly
        var types = Assembly.GetExecutingAssembly().GetTypes();

        // Find the type that has the FloomPlugin attribute with the matching package name
        var pluginType = types.FirstOrDefault(t => t.GetCustomAttributes<FloomPluginAttribute>().Any(a => a.PackageName == packageName));

        if (pluginType == null)
        {
            _logger.LogError($"Plugin {packageName} not found");
            return null;
        }

        return (IFloomPlugin)Activator.CreateInstance(pluginType);
    } 
}