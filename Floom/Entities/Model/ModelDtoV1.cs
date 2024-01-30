using FluentValidation;

namespace Floom.Entities.Model
{
    //schema: v1
    //kind: Model
    //name: docs-model
    //type: text
    //system: "You are an assitant that speaks like Shakespear" #optional
    //user: "{input}"

    public class ModelDtoV1 : MainDto
    {
        public ModelVendor vendor { get; set; } //Don't provide if custom private LLM
        public string model { get; set; }
        public string apiKey { get; set; }
        public string uri { get; set; } //for custom private LLMs

        //From DB Object to DTO
        public static ModelDtoV1 FromModel(ModelEntity modelEntity)
        {
            return new ModelDtoV1
            {
                id = modelEntity.name,
                vendor = modelEntity.vendor,
                model = modelEntity.model,
                apiKey = modelEntity.apiKey,
                uri = modelEntity.uri
            };
        }

        //From DTO to DB Object
        public ModelEntity ToEntity()
        {
            return new ModelEntity
            {
                name = this.id,
                vendor = this.vendor,
                model = this.model,
                apiKey = this.apiKey,
                uri = this.uri
            };
        }
    }

    public class ModelDtoV1Validator : AbstractValidator<ModelDtoV1?>
    {
        public ModelDtoV1Validator()
        {
            RuleFor(dto => dto).NotNull().DependentRules(() =>
            {
                RuleFor(dto => dto!.id).NotEmpty().WithMessage("'id' is missing");
                RuleFor(dto => dto!.model).NotEmpty().WithMessage("'model' is missing");
            });
        }
    }
}