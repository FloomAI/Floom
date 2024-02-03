namespace Floom.Model;

public class FloomPromptRequest
{
    public string? system { get; set; }
    public string? user { get; set; }
    public List<FloomPromptMessage> previousMessages { get; set; } = new List<FloomPromptMessage>();

    //Image
    public string resolution { get; set; }
    public uint? options { get; set; } = 1;
}

public class FloomPromptMessage
{
    public string role { get; set; } = string.Empty;
    public string content { get; set; } = string.Empty;
}