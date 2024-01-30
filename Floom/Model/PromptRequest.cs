namespace Floom.Model;

public class PromptRequest
{
    public string? system { get; set; }
    public string? user { get; set; }
    public List<PromptMessage> previousMessages { get; set; } = new List<PromptMessage>();

    //Image
    public string resolution { get; set; }
    public uint? options { get; set; } = 1;
}

public class PromptMessage
{
    public string role { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
}