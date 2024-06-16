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

    public string userId { get; set; }
}