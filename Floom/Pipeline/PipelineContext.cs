using Floom.Pipeline.Entities.Dtos;
using Floom.Plugin;

namespace Floom.Pipeline;

public enum PipelineExecutionStatus
{
    NotStarted,
    InProgress,
    Completed,
    Events
}

public enum PipelineExecutionStage
{
    Init,
    Model,
    Prompt,
    Response
}

public abstract class PipelineEvent
{
    public DateTime Timestamp { get; set; }
    public PluginResult Result { get; set; }
}

public class PipelineContext
{
    public string PipelineName { get; set; }
    
    public FloomRequest Request { get; set; }
    public PipelineExecutionStatus Status { get; set; }
    public PipelineExecutionStage CurrentStage { get; set; }
    
    public Entities.Pipeline Pipeline { get; set; }
    
    private List<PipelineEvent> _events = new();

    // Other properties...

    public void AddEvent(PipelineEvent pipelineEvent)
    {
        _events.Add(pipelineEvent);
    }

    public IEnumerable<PipelineEvent> GetEvents() => _events.AsReadOnly();
}