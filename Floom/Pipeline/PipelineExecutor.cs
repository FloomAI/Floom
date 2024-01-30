using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Model;
using Floom.Pipeline.Prompt;
using Floom.Repository;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace Floom.Pipeline;

public interface IPipelineExecutor
{
    Task<IActionResult> Execute(FloomRequest floomRequest);
}

public class PipelineExecutor : IPipelineExecutor
{
    private readonly ILogger<PipelineExecutor> _logger;
    private readonly IRepository<PipelineEntity> _pipelineRepository;
    private readonly IModelStageHandler _modelStageHandler;
    private readonly IPromptStageHandler _promptStageHandler;

    public PipelineExecutor(
        IModelStageHandler modelStageHandler,
        IPromptStageHandler promptStageHandler,
        IRepositoryFactory repositoryFactory, 
        ILogger<PipelineExecutor> logger)
    {
        _logger = logger;
        _pipelineRepository = repositoryFactory.Create<PipelineEntity>("pipelines");
        _modelStageHandler = modelStageHandler;
        _promptStageHandler = promptStageHandler;
    }

    public async Task<IActionResult> Execute(FloomRequest floomRequest)
    {
        _logger.LogInformation($"Starting Pipeline Execution: {floomRequest.pipelineId}");

        var pipelineContext = new PipelineContext()
        {
            PipelineName = floomRequest.pipelineId,
            Status = PipelineExecutionStatus.NotStarted,
            CurrentStage = PipelineExecutionStage.Init
        };

        var pipeline = await _pipelineRepository.Get(floomRequest.pipelineId, "name");
        
        if (pipeline == null)
        {
            throw new InvalidOperationException($"Pipeline with ID {floomRequest.pipelineId} not found. Cannot execute {pipelineContext}");
        }

        pipelineContext.Request = floomRequest;
        pipelineContext.Pipeline = pipeline.ToModel();
        
        // Status and CurrentStage needs to be changed to events
        pipelineContext.Status = PipelineExecutionStatus.InProgress;

        // Prompt Stage
        pipelineContext.CurrentStage = PipelineExecutionStage.Prompt;
        
        // handle prompt stage
        await _promptStageHandler.ExecuteAsync(pipelineContext);
        
        // Model Stage
        pipelineContext.CurrentStage = PipelineExecutionStage.Model;
        
        // handle model stage
        await _modelStageHandler.ExecuteAsync(pipelineContext);
        
        var promptResponseEvents = pipelineContext.GetEvents().OfType<ModelConnectorResultEvent>();
        
        var promptResponse = promptResponseEvents.FirstOrDefault()?.Response;
        
        var floomResponse = new FloomResponse()
        {
            messageId = "",
            chatId = "",
            values = promptResponse?.values,
            processingTime = promptResponse.elapsedProcessingTime,
            tokenUsage = promptResponse.tokenUsage == null
                ? new FloomResponseTokenUsage()
                : promptResponse.tokenUsage.ToFloomResponseTokenUsage()
        };

        _logger.LogInformation($"Completing Pipeline Execution: {floomRequest.pipelineId}");

        pipelineContext.Status = PipelineExecutionStatus.Completed;

        return new OkObjectResult(floomResponse);
    }
}