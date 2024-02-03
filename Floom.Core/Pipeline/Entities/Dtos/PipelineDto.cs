using FluentValidation;

namespace Floom.Pipeline.Entities.Dtos;

public class PipelineDto
{
    public string Kind { get; set; }
    public PipelineDetailsDto Pipeline { get; set; }

    public class PipelineDetailsDto
    {
        public string Name { get; set; }
        public IEnumerable<PluginConfigurationDto>? Model { get; set; }
        public PromptStageDto? Prompt { get; set; }
        public ResponseStageDto? Response { get; set; }
        public IEnumerable<PluginConfigurationDto>? Global { get; set; }
    }

    public class PromptStageDto
    {
        public PluginConfigurationDto? Template { get; set; }
        public IEnumerable<PluginConfigurationDto>? Context { get; set; }
        public IEnumerable<PluginConfigurationDto>? Optimization { get; set; }
        public IEnumerable<PluginConfigurationDto>? Validation { get; set; }
    }

    public class ResponseStageDto
    {
        public IEnumerable<PluginConfigurationDto>? Format { get; set; }
        public IEnumerable<PluginConfigurationDto>? Validation { get; set; }
    }

    public class PluginConfigurationDto
    {
        public string Package { get; set; }
        public Dictionary<string, object> Configuration { get; set; }

        public PluginConfigurationDto()
        {
            Configuration = new Dictionary<string, object>();
        }
    }

    public class PipelineDtoValidator : AbstractValidator<PipelineDto>
    {
        public PipelineDtoValidator()
        {
            RuleFor(dto => dto.Pipeline.Name).NotEmpty();
        }
    }
}