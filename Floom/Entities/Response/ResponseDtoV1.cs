using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Entities.Response
{
    //schema: v1
    //kind: Response
    //name: docs-response
    //type: text
    //language: English
    //maxSentences: 3
    //maxCharacters: 1500
    //temperature: 0.9

    public class ResponseDtoV1 : MainDto
    {
        [BsonRepresentation(BsonType.String)] public ResponseType type { get; set; } //To Enum
        public string? language { get; set; }
        public uint maxSentences { get; set; }
        public uint maxCharacters { get; set; }
        public double temperature { get; set; }
        public List<string>? examples { get; set; }
        public bool referToData { get; set; }


        //image
        public string? resolution { get; set; }
        public uint? options { get; set; }
        public string? format { get; set; }
        public double? quality { get; set; }

        public static ResponseDtoV1 FromEntity(ResponseEntity responseEntity)
        {
            return new ResponseDtoV1
            {
                id = responseEntity.name,
                type = responseEntity.type,
                language = responseEntity.language,
                maxSentences = responseEntity.maxSentences,
                maxCharacters = responseEntity.maxCharacters,
                temperature = responseEntity.temperature,
                examples = responseEntity.examples,
                referToData = responseEntity.referToData,
                //
                // //Image
                resolution = responseEntity.resolution,
                options = responseEntity.options,
                format = responseEntity.format,
                quality = responseEntity.quality
            };
        }

        //From DTO to DB Object
        public ResponseEntity ToEntity()
        {
            return new ResponseEntity
            {
                name = id,
                type = type,
                language = language,
                maxSentences = maxSentences,
                maxCharacters = maxCharacters,
                temperature = temperature,
                examples = examples,
                referToData = referToData,

                //Image
                resolution = resolution,
                options = options,
                format = format,
                quality = quality
            };
        }
    }

    public class ResponseDtoV1Validator : AbstractValidator<ResponseDtoV1?>
    {
        public ResponseDtoV1Validator()
        {
            RuleFor(dto => dto).NotNull().DependentRules(() =>
            {
                RuleFor(dto => dto!.id).NotEmpty().WithMessage("'id' is missing");
            });
        }
    }
}