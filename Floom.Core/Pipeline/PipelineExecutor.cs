using System.Net;
using Floom.Auth;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.StageHandler.Model;
using Floom.Pipeline.StageHandler.Prompt;
using Floom.Repository;
using Floom.Utils;

namespace Floom.Pipeline;

public interface IPipelineExecutor
{
    Task<FloomResponseBase> Execute(FloomRequest floomRequest);
}

public class PipelineExecutor : IPipelineExecutor
{
    private readonly ILogger<PipelineExecutor> _logger;
    private readonly IRepository<PipelineEntity> _pipelineRepository;
    private readonly IRepository<UserEntity> _usersRepository;
    private readonly IModelStageHandler _modelStageHandler;
    private readonly IPromptStageHandler _promptStageHandler;

    public PipelineExecutor(
        IModelStageHandler modelStageHandler,
        IPromptStageHandler promptStageHandler,
        IRepositoryFactory repositoryFactory, 
        ILogger<PipelineExecutor> logger)
    {
        _logger = logger;
        _pipelineRepository = repositoryFactory.Create<PipelineEntity>();
        _usersRepository = repositoryFactory.Create<UserEntity>();
        _modelStageHandler = modelStageHandler;
        _promptStageHandler = promptStageHandler;
    }

    public async Task<FloomResponseBase> Execute(FloomRequest floomRequest)
    {
        _logger.LogInformation($"Starting Pipeline Execution: {floomRequest.pipelineId}");

        var pipelineContext = new PipelineContext()
        {
            PipelineName = floomRequest.pipelineId,
            Status = PipelineExecutionStatus.NotStarted,
            CurrentStage = PipelineExecutionStage.Init
        };
        
        var httpRequestApiKey = HttpContextHelper.GetApiKeyFromHttpContext();

        var pipeline = await _pipelineRepository.FindByCondition(a => a.name == floomRequest.pipelineId && (string?)a.createdBy["apiKey"] == httpRequestApiKey);

        if (floomRequest.username != null)
        {
            _logger.LogInformation($"Pipeline Execution: User {floomRequest.username} is trying to execute pipeline {floomRequest.pipelineId}");
            
            var httpRequestUserId = HttpContextHelper.GetUserIdFromHttpContext();

            var user = await _usersRepository.Get(httpRequestUserId, "_id");

            if (user != null)
            {
                if(user.username != floomRequest.username)
                {
                    var errorMessage = $"User {floomRequest.username} is not authorized to execute pipeline {floomRequest.pipelineId}";
                    _logger.LogError(errorMessage);
                    return new FloomPipelineErrorResponse
                    {
                        success = false,
                        message = errorMessage,
                        statusCode = HttpStatusCode.Unauthorized
                    };
                }
                else
                {
                    _logger.LogInformation($"Pipeline Execution: User {floomRequest.username} is executing pipeline {floomRequest.pipelineId}");
                }
            }
            else
            {
                var errorMessage = $"User {floomRequest.username} not found.";
                _logger.LogError(errorMessage);
                return new FloomPipelineErrorResponse()
                {
                    success = false,
                    message = errorMessage,
                    statusCode = HttpStatusCode.Unauthorized
                };
            }
        }
        
        if (pipeline == null)
        {
            var errorMessage = $"Pipeline with ID {floomRequest.pipelineId} not found.";
            _logger.LogError(errorMessage);
            return new FloomPipelineErrorResponse()
            {
                success = false,
                message = errorMessage,
                statusCode = HttpStatusCode.NotFound
            };
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

        FloomResponseBase? floomResponse;
        
        if (promptResponse == null)
        {
            _logger.LogError($"Model Stage: No response from model connector");
            floomResponse = new FloomPipelineErrorResponse()
            {
                success = false,
                message = "No response from model connector",
                statusCode = HttpStatusCode.BadRequest
            };
        }
        else if(promptResponse.success == false)
        {
            _logger.LogError($"Model Stage: Failed: {promptResponse.message}");
            floomResponse = new FloomPipelineErrorResponse()
            {
                success = false,
                message = promptResponse.message,
                errorCode = promptResponse.errorCode,
                statusCode = HttpStatusCode.BadRequest
            };
        }
        else
        {
            floomResponse = new FloomResponse()
            {
                messageId = "",
                chatId = "",
                values = promptResponse?.values,
                processingTime = promptResponse.elapsedProcessingTime,
                tokenUsage = promptResponse.tokenUsage == null
                    ? new FloomResponseTokenUsage()
                    : promptResponse.tokenUsage.ToFloomResponseTokenUsage()
            };
        }
        
        _logger.LogInformation($"Completing Pipeline Execution: {floomRequest.pipelineId}");

        pipelineContext.Status = PipelineExecutionStatus.Completed;

        return floomResponse;
    }
}