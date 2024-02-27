using Floom.Events;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Floom.Repository;
using Floom.Utils;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Pipeline;

public interface IPipelineCommitter
{
    Task<IActionResult> Commit(PipelineDto pipelineDto);
}

public class PipelineCommitter : IPipelineCommitter
{
    private readonly IRepository<PipelineEntity> _repository;
    private readonly EventsManager _eventsManager;

    public PipelineCommitter(IRepositoryFactory repositoryFactory, EventsManager eventsManager)
    {
        _repository = repositoryFactory.Create<PipelineEntity>();
        _eventsManager = eventsManager;
    }
    
    /**
     * Upon receiving a pipeline, the PipelineCommitter will:
     * 1. Check if pipeline is existing or new
     * 2. If existing, replace pipeline with new pipeline
     * 3. If new, create new pipeline
     * 4. Save pipeline into database
     * 5. Update Plugin Registry with plugins used in pipeline
     * 6. Pass 'OnCommit' event to Events Dispatcher with given plugin and pipeline context
     */
    public async Task<IActionResult> Commit(PipelineDto pipelineDto)
    {
        var existingPipeline = await _repository.Get(pipelineDto.Pipeline.Name, "name");

        if (existingPipeline != null)
        {
            await _repository.Delete(pipelineDto.Pipeline.Name, "name");
        }

        var pipelineEntity = pipelineDto.ToEntity();
        pipelineEntity.userId = HttpContextHelper.GetUserIdFromHttpContext();
        
        pipelineEntity.AddCreatedByOwner("floom-user");
        
        await _repository.Insert(pipelineEntity);
        
        // Update Events Manager with plugins used in pipeline
        
        await _eventsManager.OnPipelineCommit(pipelineEntity);
        
        return new OkObjectResult(new { Message = $"Pipeline Committed {pipelineDto.Pipeline.Name}" });
    }
}