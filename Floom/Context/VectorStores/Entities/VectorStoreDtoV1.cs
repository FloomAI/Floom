using Floom.VectorStores;
using FluentValidation;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Floom.Entities.VectorStore
{
    //schema: v1
    //kind: VectorStore
    //name: docs-vectorstore
    //vendor: Pinecone
    //apiKey: 824jf285hg828gj2g951gh18

    public class VectorStoreDtoV1 : MainDto
    {
        public VectorStoreVendor.Enum vendor { get; set; }
        public string? apiKey { get; set; }
        public string? environment { get; set; }
        public string? endpoint { get; set; } //milvus
        public int port { get; set; } //milvus
        public string? username { get; set; } //milvus
        public string? password { get; set; } //milvus


        //From DTO to DB Object
        public VectorStoreModel ToModel()
        {
            return new VectorStoreModel
            {
                name = id,
                Vendor = vendor,
                ConnectionArgs = new VectorStoreModel.VectorStoreConnectionArgs()
                {
                    ApiKey = apiKey,
                    Environment = environment,
                    Endpoint = endpoint,
                    Port = (ushort)port,
                    Username = username,
                    Password = password
                }
            };
        }
    }

    public class VectorStoreDtoV1Validator : AbstractValidator<VectorStoreDtoV1?>
    {
        public VectorStoreDtoV1Validator()
        {
            RuleFor(dto => dto).NotNull().DependentRules(() =>
            {
                RuleFor(dto => dto!.id).NotEmpty().WithMessage("'id' is missing");
            });
        }
    }
}