using Floom.Entities;
using Floom.Entities.Prompt;
using FluentValidation;

namespace Floom.Models
{
    //schema: v1
    //kind: Prompt
    //name: docs-prompt
    //type: text
    //system: "You are an assitant that speaks like Shakespear" #optional
    //user: "{input}"

    public class PromptDtoV1 : MainDto
    {
        public PromptType type { get; set; } //To Enum
        public string? system { get; set; }
        public string? user { get; set; }

        //From DB Object to DTO
        public static PromptDtoV1 FromEntity(PromptEntity promptEntity)
        {
            return new PromptDtoV1
            {
                id = promptEntity.name,
                type = promptEntity.type,
                system = promptEntity.system,
                user = promptEntity.user
            };
        }

        //From DTO to DB Object
        public PromptEntity ToEntity()
        {
            return new PromptEntity
            {
                name = this.id,
                type = PromptType.Text, //To Enumable
                system = this.system,
                user = this.user
            };
        }
    }

    public class PromptDtoV1Validator : AbstractValidator<PromptDtoV1?>
    {
        public PromptDtoV1Validator()
        {
            RuleFor(dto => dto).NotNull().DependentRules(() =>
            {
                RuleFor(dto => dto!.id).NotEmpty().WithMessage("'id' is missing");
            });
        }
    }
}