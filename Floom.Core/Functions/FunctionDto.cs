public class FunctionDto
{
    public string name { get; set; }
    public string description { get; set; }
    public string runtimeLanguage { get; set; }
    public string runtimeFramework { get; set; }
    public string author { get; set; }
    public string username { get; set; }
    public string version { get; set; }
    public int rating { get; set; }
    public List<int> downloads { get; set; }
    public List<ParameterDto> parameters { get; set; } = new();
}

public class FeaturedFunctionDto
{
    public string id { get; set; }
    public string name { get; set; }
    public string slug { get; set; }
    public string description { get; set; }
    public string runtimeLanguage { get; set; }
    public string runtimeFramework { get; set; }
    public string author { get; set; }
    public string version { get; set; }
    public int rating { get; set; }
    public List<int> downloads { get; set; }
    public List<ParameterDto> parameters { get; set; } = new();
}

public class ParameterDto
{
    public string name { get; set; }
    public string? description { get; set; }
    public bool required { get; set; }
    public object? defaultValue { get; set; }
}
