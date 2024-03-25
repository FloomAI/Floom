namespace Floom.Pipeline.Entities.Dtos;

public class RunFloomPipelineRequest
{
    public string? pipelineId { get; set; } //Pipeline ID
    public string? username { get; set; } //User Name
    public string? prompt { get; set; } = ""; //User Input
    public Dictionary<string, string>? variables { get; set; } //Vars
    public IFormFile? file { get; set; }
    public object? responseType { get; set; }
}