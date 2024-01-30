using Floom.Misc;
using FluentValidation;

namespace Floom.Models
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
        public string type { get; set; } //To Enum
        public string language { get; set; }
        public uint maxSentences { get; set; }
        public uint maxCharacters { get; set; }
        public double temperature { get; set; }
        public List<string> examples { get; set; }
        public bool referToData { get; set; }


        //image
        public string resolution { get; set; }
        public uint options { get; set; }
        public string format { get; set; }
        public double quality { get; set; }

        public static ResponseDtoV1 FromResponse(Response response)
        {
            return new ResponseDtoV1
            {
                id = response.name,
                type = Response.ConvertFromResponseType(response.type),
                language = response.language,
                maxSentences = response.maxSentences,
                maxCharacters = response.maxCharacters,
                temperature = response.temperature,
                examples = response.examples,
                referToData = response.referToData,

                //Image
                resolution = response.resolution,
                options = response.options,
                format = response.format,
                quality = response.quality
            };
        }

        //From DTO to DB Object
        public Response ToResponse()
        {
            return new Response
            { 
                name = this.id,
                //type = ResponseType.Text, //To Enumable
                type = Response.ConvertToResponseType(this.type),
                language =  this.language,
                maxSentences = this.maxSentences,
                maxCharacters = this.maxCharacters,
                temperature = this.temperature,
                examples = this.examples,
                referToData = this.referToData,

                //Image
                resolution = this.resolution,
                options = this.options,
                format = this.format,
                quality = this.quality
            };
        }
    }

    public class ResponseDtoV1Validator : AbstractValidator<ResponseDtoV1>
    {
        public ResponseDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}