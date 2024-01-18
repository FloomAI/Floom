using Floom.Misc;
using FluentValidation;

namespace Floom.Models
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
        public string type { get; set; } //To Enum
        public string vendor { get; set; } //if private ai model, don't supply provider at all - only supply URL, port, creds, api protocol
        public string model { get; set; }
        public string apiKey { get; set; }
        public string url { get; set; }

        //From DB Object to DTO
        public static EmbeddingsDtoV1 FromEmbeddings(Embeddings embeddings)
        {
            return new EmbeddingsDtoV1
            {
                id = embeddings.name,
                type = embeddings.type.ToString(), //To Enumable
                vendor = embeddings.vendor,
                apiKey = embeddings.apiKey,
                url = embeddings.url
            };
        }

        //From DTO to DB Object
        public Embeddings ToEmbeddings()
        {
            return new Embeddings
            {
                name = this.id,
                type = EmbeddingsType.Text, //To Enumable
                vendor = this.vendor,
                apiKey = this.apiKey,
                url = this.url
            };
        }
    }

    public class EmbeddingsDtoV1Validator : AbstractValidator<EmbeddingsDtoV1>
    {
        public EmbeddingsDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}
