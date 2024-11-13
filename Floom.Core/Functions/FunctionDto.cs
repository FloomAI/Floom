namespace Floom.Functions;

public class BaseFunctionDto
{
    /**
    packageName = unique = function url
    */
    public string name { get; set; }
    public string author { get; set; }
    public double rating { get; set; }
    public List<int> downloads { get; set; } = new();

    public string runtimeLanguage { get; set; }
    public string runtimeFramework { get; set; }
    public string version { get; set; }
    public List<ParameterDto> parameters { get; set; } = new();

    public TranslatedField title { get; set; }
    public TranslatedField description { get; set; }

    public TranslatedField promptPlaceholder { get; set; }
}

public class ParameterDto
{
    public string name { get; set; }
    public TranslatedField? description { get; set; }
    public bool required { get; set; }
    public object? defaultValue { get; set; }
}