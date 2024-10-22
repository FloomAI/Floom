using Floom.Functions;

public class FunctionDto
{
    public string name { get; set; }
    public string description { get; set; }
    public string runtimeLanguage { get; set; }
    public string runtimeFramework { get; set; }
    public string author { get; set; }
    public string username { get; set; }
    public string version { get; set; }
    public double rating { get; set; }
    public List<int> downloads { get; set; }
    public List<ParameterDto> parameters { get; set; } = new();
}

public class SearchResultFunctionDto
{
    public string id { get; set; }
    public string name { get; set; }
    public string slug { get; set; }
}
    
public class FeaturedFunctionDto
{
    public string id { get; set; }
    public string name { get; set; }
    public string slug { get; set; }
    public TranslatedField title { get; set; } // Translated titles
    public TranslatedField description { get; set; } // Translated descriptions
    public TranslatedField promptPlaceholder { get; set; } // Translated descriptions
    public string runtimeLanguage { get; set; }
    public string runtimeFramework { get; set; }
    public string author { get; set; }
    public string version { get; set; }
    public double rating { get; set; }
    public List<int> downloads { get; set; }
    public List<FeaturedFunctionParameterDto> parameters { get; set; } = new();
}

public class ParameterDto
{
    public string name { get; set; }
    public string? description { get; set; }
    public bool required { get; set; }
    public object? defaultValue { get; set; }
}

public class FeaturedFunctionParameterDto
{
    public string name { get; set; }
    public TranslatedField? description { get; set; }
    public bool required { get; set; }
    public object? defaultValue { get; set; }
}

