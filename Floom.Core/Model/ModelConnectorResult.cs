using Floom.Pipeline.Entities.Dtos;

namespace Floom.Model;

public class ModelConnectorResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int? ErrorCode { get; set; }
    public ResponseValue Data { get; set; }
    
}