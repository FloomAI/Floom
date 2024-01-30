using Floom.Misc;
using FluentValidation;

namespace Floom.Models
{
    //schema: v1
    //kind: Model
    //name: docs-model
    //type: text
    //system: "You are an assitant that speaks like Shakespear" #optional
    //user: "{input}"

    public class ModelDtoV1 : MainDto
    {
        public string vendor { get; set; } //Don't provide if custom private LLM
        public string model { get; set; }
        public string apiKey { get; set; }
        public string uri { get; set; } //for custom private LLMs

        //From DB Object to DTO
        public static ModelDtoV1 FromModel(Model model)
        {
            return new ModelDtoV1
            {
                id = model.name,
                vendor = model.vendor,
                model = model.model,
                apiKey = model.apiKey,
                uri = model.uri
            };
        }

        //From DTO to DB Object
        public Model ToModel()
        {
            return new Model
            {
                name = this.id,
                vendor = this.vendor,
                model = this.model,
                apiKey = this.apiKey,
                uri = this.uri
            };
        }
    }

    public class ModelDtoV1Validator : AbstractValidator<ModelDtoV1>
    {
        public ModelDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}
