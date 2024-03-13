namespace Floom.Pipeline.StageHandler;

public interface IStageHandler
{
    Task ExecuteAsync(PipelineContext pipelineContext);
}