namespace Floom.Pipeline.Entities.Dtos;

public class FloomRequest
{
    public string? pipelineId { get; set; } //Pipeline ID
    public string? username { get; set; } //User Name
    public string chatId { get; set; } = ""; //Chat ID
    public string? input { get; set; } = ""; //User Input
    public Dictionary<string, string>? variables { get; set; } //Vars
    public DataTransferType dataTransfer { get; set; }
    public IFormFile? file { get; set; }
}

public enum DataTransferType
{
    Base64 = 1,
    //Url = 2
}