using Floom.Base;
using Floom.Repository;

[CollectionName("functions")]
public class FunctionEntity: DatabaseEntity
{
    public string name { get; set; }
    public string runtimeLanguage { get; set; }
    public string runtimeFramework { get; set; }
    public string promptUrl { get; set; }
    public string? dataUrl { get; set; }
    public string? description { get; set; }
    public string userId { get; set; }
    public string[]? roles { get; set; }

    public string version { get; set; } // Version of the function
    public int rating { get; set; } // Rating (e.g., 4 out of 5)
    public List<int> downloads { get; set; } = new(); // Download statistics over time
    public List<Parameter> parameters { get; set; } = new(); // Parameters required for the function
}

public class Parameter
{
    public string name { get; set; }
    public string? description { get; set; }
    public bool required { get; set; }
    public string? defaultValue { get; set; }
}

public static class Roles
{
    public const string Public = "public";
    public const string Featured = "featured";
}