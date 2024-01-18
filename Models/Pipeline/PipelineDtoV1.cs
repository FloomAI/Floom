using Azure;
using Floom.Misc;
using FluentValidation;

namespace Floom.Models
{
    //schema: v1
    //kind: Pipeline
    //name: docs-pipeline
    //model: docs-model
    //prompt: docs-prompt
    //response: docs-response
    //chatHistory: true
    //data:
    //- docs-data

    public class PipelineDtoV1 : MainDto
    {
        public string model { get; set; }
        public string prompt { get; set; }
        public string response { get; set; }
        public bool chatHistory { get; set; }
        public List<string> data { get; set; }


        //From DB Object to DTO
        public static PipelineDtoV1 FromPipeline(Pipeline pipeline)
        {
            return new PipelineDtoV1
            {
                model = pipeline.model,
                prompt = pipeline.prompt,
                response = pipeline.response,
                chatHistory = pipeline.chatHistory,
                data = pipeline.data
            };
        }

        //From DTO to DB Object
        public Pipeline ToPipeline()
        {
            return new Pipeline
            {
                name = this.id,
                model = this.model,
                prompt = this.prompt,
                response = this.response,
                chatHistory = this.chatHistory,
                data = this.data
            };
        }
    }

    public class PipelineDtoV1Validator : AbstractValidator<PipelineDtoV1>
    {
        public PipelineDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}
