using Floom.Misc;
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
        public string type { get; set; } //To Enum
        public string system { get; set; }
        public string user { get; set; }

        //From DB Object to DTO
        public static PromptDtoV1 FromPrompt(Prompt prompt)
        {
            return new PromptDtoV1
            {
                id = prompt.name,
                type = prompt.type.ToString(), //To Enumable
                system = prompt.system,
                user = prompt.user
            };
        }

        //From DTO to DB Object
        public Prompt ToPrompt()
        {
            return new Prompt
            {
                name = this.id,
                type = PromptType.Text, //To Enumable
                system = this.system,
                user = this.user
            };
        }
    }

    public class PromptDtoV1Validator : AbstractValidator<PromptDtoV1>
    {
        public PromptDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}
