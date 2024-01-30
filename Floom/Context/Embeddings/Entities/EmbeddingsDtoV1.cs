using Floom.Entities;
using Floom.Entities.Embeddings;
using FluentValidation;

namespace Floom.Embeddings.Entities
{
    //schema: v1
    //kind: Embeddings
    //name: docs-embeddings
    //type: text
    //vendor: OpenAI
    //model: text-embedding-ada-002
    //apiKey: 824jf285hg828gj2g951gh18

    public class EmbeddingsDtoV1 : MainDto
    {
        public EmbeddingsType type { get; set; } //To Enum

        public ModelVendor vendor { get; set; }

        public string model { get; set; }
        public string apiKey { get; set; }
        public string url { get; set; }
        
        //From DTO to DB Object
        public EmbeddingsEntity ToEntity()
        {
            return new EmbeddingsEntity
            {
                name = id,
                type = type,
                vendor = vendor,
                apiKey = apiKey,
                url = url
            };
        }
        
        public EmbeddingsModel ToModel()
        {
            return new EmbeddingsModel
            {
                name = id,
                type = type,
                vendor = vendor,
                apiKey = apiKey,
                url = url
            };
        }
    }

    public class EmbeddingsDtoV1Validator : AbstractValidator<EmbeddingsDtoV1?>
    {
        public EmbeddingsDtoV1Validator()
        {
            RuleFor(dto => dto).NotNull().DependentRules(() =>
            {
                RuleFor(dto => dto!.id).NotEmpty().WithMessage("'id' is missing");
            });
        }
    }
}