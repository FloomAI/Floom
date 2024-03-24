using System.Net;
using Microsoft.VisualBasic.CompilerServices;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Pipeline.Entities.Dtos
{
    public class FloomResponseBase
    {
        
    }
    
    public class ResponseFormatterResult
    {
        public ResponseValue value { get; set; } = new();
    }

    public class FloomPipelineResponse : FloomResponseBase
    {
        public bool? success { get; set; }
        public ResponseValue? value { get; set; }
    }
    
    public class FloomPipelineErrorResponse : FloomResponseBase
    {
        public bool? success { get; set; }
        public string? message { get; set; } = "";
        public int? errorCode { get; set; }
        public HttpStatusCode? statusCode { get; set; }
    }
    
    public class ResponseValue
    {
        public DataType type { get; set; } = DataType.String;
        public string format { get; set; } = "";
        public object value { get; set; } = "";
    }

    public enum DataType
    {
        String = 1,
        Image = 2,
        Audio = 3,
        JsonObject = 4
    }

    public static class ResponseFormat
    {
        public static string FromDataType(DataType dataType)
        {
            return dataType switch
            {
                DataType.String => "text/plain",
                DataType.Image => "image/png",
                DataType.Audio => "audio/mp4",
                DataType.JsonObject => "application/json",
                _ => "text/plain"
            };
        }
        
        public static DataType FromString(string format)
        {
            return format switch
            {
                "text" => DataType.String,
                "text/plain" => DataType.String,
                "image" => DataType.Image,
                "image/png" => DataType.Image,
                "audio" => DataType.Audio,
                "audio/mp4" => DataType.Audio,
                "object" => DataType.JsonObject,
                "application/json" => DataType.JsonObject,
                _ => DataType.String
            };
        }
    }

    [BsonIgnoreExtraElements]
    public class FloomResponseTokenUsage
    {
        public int processingTokens { get; set; }
        public int promptTokens { get; set; }
        public int totalTokens { get; set; }
    }
}