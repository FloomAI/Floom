namespace Floom.Pipeline.Stages;

public interface IStageHandler
{
    Task ExecuteAsync(PipelineContext pipelineContext);
}