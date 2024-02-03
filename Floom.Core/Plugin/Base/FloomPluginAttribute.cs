namespace Floom.Plugin.Base;

[AttributeUsage(AttributeTargets.Class)]
public class FloomPluginAttribute : Attribute
{
    public string PackageName { get; }

    public FloomPluginAttribute(string packageName)
    {
        PackageName = packageName;
    }
}