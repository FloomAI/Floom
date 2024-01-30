using Floom.Pipeline.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Pipeline;

public interface IPipelinesService
{
    Task<IActionResult?> Commit(PipelineDto pipelineDto);
    Task<IActionResult> Run(FloomRequest floomRequest);
}

public class PipelinesService : IPipelinesService
{
    private readonly Lazy<IPipelineCommitter> _pipelineCommitter;
    private readonly Lazy<IPipelineExecutor> _pipelineExecutor;
    
    public PipelinesService(
        Lazy<IPipelineCommitter> pipelineCommitter,
        Lazy<IPipelineExecutor> pipelineExecutor)
    {
        _pipelineCommitter = pipelineCommitter;
        _pipelineExecutor = pipelineExecutor;
    }
    
    public async Task<IActionResult?> Commit(PipelineDto pipelineDto)
    {
        await _pipelineCommitter.Value.Commit(pipelineDto);
        return new OkObjectResult(new { Message = $"Pipeline Committed Successfully" });
    }

    public async Task<IActionResult> Run(FloomRequest floomRequest)
    {
        return await _pipelineExecutor.Value.Execute(floomRequest);
    }
}