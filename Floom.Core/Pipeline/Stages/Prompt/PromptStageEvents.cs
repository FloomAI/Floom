using Floom.Pipeline.Entities.Dtos;

namespace Floom.Pipeline.Stages.Prompt;

public class PromptTemplateResultEvent : PipelineEvent
{
    public PromptTemplateResult? ResultData { get; set; }
}

public class PromptTemplateResult
{
    public string UserPrompt { get; set; }
    public string? UserPromptAddon { get; set; }
    public string? SystemPrompt { get; set; }
    public DataType ResponseType { get; set; } = DataType.String;
    public string? ResponseFormat { get; set; } = "text/plain";
    public uint MaxCharacters { get; set; } = 0;
    public uint MaxSentences { get; set; } = 0;
    public string? Language { get; set; } = "en"; // ISO 639-1 code
    public byte[]? File { get; set; }
}

public class PromptContextResultEvent : PipelineEvent
{
    public PromptContextResult ResultData { get; set; }
}

public class PromptContextResult
{
    public string? Context { get; set; }
}

public class PromptValidationResultEvent : PipelineEvent
{
    public object? ResultData { get; set; }
}

public class PromptStageResultEvent : PipelineEvent
{
    public FloomRequest? ResultData { get; set; }
}

public class FloomRequest
{
    public PromptTemplateResult Prompt;
    public PromptContextResult? Context;
}