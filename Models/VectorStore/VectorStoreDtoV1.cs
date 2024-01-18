using Floom.Misc;
using FluentValidation;

namespace Floom.Models
{
    //schema: v1
    //kind: VectorStore
    //name: docs-vectorstore
    //vendor: Pinecone
    //apiKey: 824jf285hg828gj2g951gh18

    public class VectorStoreDtoV1 : MainDto
    {
        public string vendor { get; set; } //if private vdb, don't supply provider at all - only supply URL, port, creds, api protocol
        public string apiKey { get; set; }
        public string environment { get; set; }
        public string endpoint { get; set; } //milvus
        public int port { get; set; } //milvus
        public string username { get; set; } //milvus
        public string password { get; set; } //milvus

        //From DB Object to DTO
        public static VectorStoreDtoV1 FromVectorStore(VectorStore vectorStore)
        {
            return new VectorStoreDtoV1
            {
                id = vectorStore.name,
                vendor = vectorStore.vendor,
                apiKey = vectorStore.apiKey,
                environment = vectorStore.environment,
                endpoint = vectorStore.endpoint,
                port = vectorStore.port,
                username = vectorStore.username,
                password = vectorStore.password
            };
        }

        //From DTO to DB Object
        public VectorStore ToVectorStore()
        {
            return new VectorStore
            {
                name = this.id,
                vendor = this.vendor,
                apiKey = this.apiKey,
                environment = this.environment,
                endpoint = this.endpoint,
                port = this.port,
                username = this.username,
                password = this.password
            };
        }
    }

    public class VectorStoreDtoV1Validator : AbstractValidator<VectorStoreDtoV1>
    {
        public VectorStoreDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}
