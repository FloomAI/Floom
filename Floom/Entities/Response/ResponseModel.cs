using MongoDB.Bson;

namespace Floom.Entities.Response;

public class ResponseModel : BaseModel
{
    public ResponseType type { get; set; }
    public string? language { get; set; }
    public uint maxSentences { get; set; }
    public uint maxCharacters { get; set; }
    public double temperature { get; set; }
    public List<string>? examples { get; set; }

    public bool referToData { get; set; }

    // //image
    public string? resolution { get; set; }
    public uint? options { get; set; }
    public string? format { get; set; }
    public double? quality { get; set; }
    
    
    public static ResponseModel FromEntity(ResponseEntity responseEntity)
    {
        return new ResponseModel
        {
            Id = responseEntity.Id == ObjectId.Empty ? null : responseEntity.Id.ToString(),
            type = responseEntity.type,
            language = responseEntity.language,
            maxSentences = responseEntity.maxSentences,
            maxCharacters = responseEntity.maxCharacters,
            temperature = responseEntity.temperature,
            examples = responseEntity.examples,
            referToData = responseEntity.referToData,
            resolution = responseEntity.resolution,
            options = responseEntity.options,
            format = responseEntity.format,
            quality = responseEntity.quality
        };
    }
}