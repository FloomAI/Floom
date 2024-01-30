namespace Floom.Plugin;

[AttributeUsage(AttributeTargets.Class)]
public class FloomPluginAttribute : Attribute
{
    public string PackageName { get; }

    public FloomPluginAttribute(string packageName)
    {
        PackageName = packageName;
    }
}