using Floom.Entities;
using FluentValidation;

namespace Floom.Data.Entities
{
    //schema: v1
    //kind: Data
    //name: docs-data
    //type: file
    //path: /dev/test/documentation.pdf -> fileId
    //split: pages
    //embeddings: docs-embeddings
    //vectorStore: docs-vectorstore

    public class DataDto : MainDto
    {
        public DataType type { get; set; } //To Enum
        public string? assetId { get; set; }
        public SplitType split { get; set; } = SplitType.Pages;
        public string? embeddings { get; set; }
        public string? vectorStore { get; set; }
        public string? model { get; set; }

        //From DB Object to DTO
        public static DataDto FromData(DataEntity dataEntity)
        {
            return new DataDto
            {
                id = dataEntity.name,
                type = dataEntity.type,
                assetId = dataEntity.assetId,
                split = dataEntity.split,
                embeddings = dataEntity.embeddings,
                vectorStore = dataEntity.vectorStore
            };
        }

        //From DTO to DB Object
        public DataEntity ToEntity()
        {
            return new DataEntity
            {
                name = id,
                type = type,
                assetId = assetId,
                split = split,
                embeddings = embeddings,
                vectorStore = vectorStore
            };
        }
    }

    public class DataDtoValidator : AbstractValidator<DataDto>
    {
        public DataDtoValidator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}