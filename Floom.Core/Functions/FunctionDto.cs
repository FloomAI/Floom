public class FunctionDto
{
    public string name { get; set; }
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
    public string? defaultValue { get; set; }
}
