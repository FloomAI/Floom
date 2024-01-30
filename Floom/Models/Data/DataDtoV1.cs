using Floom.Misc;
using FluentValidation;

namespace Floom.Models
{
    //schema: v1
    //kind: Data
    //name: docs-data
    //type: file
    //path: /dev/test/documentation.pdf -> fileId
    //split: pages
    //embeddings: docs-embeddings
    //vectorStore: docs-vectorstore

    public class DataDtoV1 : MainDto
    {
        public string type { get; set; } //To Enum
        public string fileId { get; set; }
        public string split { get; set; } //To Enum
        public string embeddings { get; set; }
        public string vectorStore { get; set; }

        //From DB Object to DTO
        public static DataDtoV1 FromData(Data data)
        {
            return new DataDtoV1
            {
                id = data.name,
                type = data.type.ToString(),
                fileId = data.fileId,
                split = data.split.ToString(),
                embeddings = data.embeddings,
                vectorStore = data.vectorStore
            };
        }

        //From DTO to DB Object
        public Data ToData()
        {
            return new Data
            {
                name = id,
                type = DataType.File, //Make Enumable
                fileId = fileId,
                split = SplitType.Pages, //Make Enumable
                embeddings = embeddings,
                vectorStore = vectorStore
            };
        }
    }

    public class DataDtoV1Validator : AbstractValidator<DataDtoV1>
    {
        public DataDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();
        }
    }
}
