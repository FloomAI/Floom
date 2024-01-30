using Floom.Data.Entities;
using Floom.Embeddings.Entities;
using Floom.Entities;
using Floom.Entities.Model;
using Floom.Entities.Response;
using Floom.Entities.VectorStore;
using Floom.Models;
using FluentValidation;

namespace Floom.Pipeline.Entities.Dtos;

public class ApplyPipelineDtoV1 : MainDto
{
    public new string schema { get; set; } = "v1";
    public List<ModelDtoV1>? models;
    public PromptDtoV1? prompt;
    public List<DataDto>? data;
    public EmbeddingsDtoV1? embeddings;
    public VectorStoreDtoV1? stores;
    public ResponseDtoV1? responses;

    public class ApplyPipelineDtoV1Validator : AbstractValidator<ApplyPipelineDtoV1>
    {
        public ApplyPipelineDtoV1Validator()
        {
            RuleFor(dto => dto.id).NotEmpty();

            RuleForEach(dto => dto.models)
                .SetValidator(new ModelDtoV1Validator())
                .When(dto => dto.models != null);

            RuleFor(dto => dto.prompt)
                .SetValidator(new PromptDtoV1Validator())
                .When(dto => dto.prompt != null);

            RuleForEach(dto => dto.data)
                .SetValidator(new DataDtoValidator())
                .When(dto => dto.data != null);

            RuleFor(dto => dto.embeddings)
                .SetValidator(new EmbeddingsDtoV1Validator())
                .When(dto => dto.embeddings != null);

            RuleFor(dto => dto.stores)
                .SetValidator(new VectorStoreDtoV1Validator())
                .When(dto => dto.stores != null);

            RuleFor(dto => dto.responses)
                .SetValidator(new ResponseDtoV1Validator())
                .When(dto => dto.responses != null);
        }
    }
}