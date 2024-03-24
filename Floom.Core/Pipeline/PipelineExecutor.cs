using System.Net;
using Floom.Auth;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Model;
using Floom.Pipeline.Stages.Prompt;
using Floom.Pipeline.Stages.Response;
using Floom.Plugin.Context;
using Floom.Repository;
using Floom.Utils;

namespace Floom.Pipeline;

public interface IPipelineExecutor
{
    Task<FloomResponseBase> Execute(RunFloomPipelineRequest floomRequest);
}

public class PipelineExecutor : IPipelineExecutor
{
    private readonly ILogger<PipelineExecutor> _logger;
    private readonly IRepository<PipelineEntity> _pipelineRepository;
    private readonly IRepository<UserEntity> _usersRepository;
    private readonly IModelStageHandler _modelStageHandler;
    private readonly IPromptStageHandler _promptStageHandler;
    private readonly IResponseStageHandler _responseStageHandler;
    private readonly IPluginContextCreator _pluginContextCreator;
    
    public PipelineExecutor(
        IModelStageHandler modelStageHandler,
        IPromptStageHandler promptStageHandler,
        IResponseStageHandler responseStageHandler,
        IRepositoryFactory repositoryFactory,
        IPluginContextCreator pluginContextCreator,
        ILogger<PipelineExecutor> logger)
    {
        _logger = logger;
        _pipelineRepository = repositoryFactory.Create<PipelineEntity>();
        _usersRepository = repositoryFactory.Create<UserEntity>();
        _modelStageHandler = modelStageHandler;
        _promptStageHandler = promptStageHandler;
        _responseStageHandler = responseStageHandler;
        _pluginContextCreator = pluginContextCreator;
    }

    public async Task<FloomResponseBase> Execute(RunFloomPipelineRequest floomRequest)
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
        
        pipelineContext.pipelineRequest = floomRequest;
        pipelineContext.Pipeline = await pipeline.ToModel(_pluginContextCreator);
        
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
        
        var modelConnectorEvents = pipelineContext.GetEvents().OfType<ModelStageResultEvent>();
        
        var modelConnectorResponse = modelConnectorEvents.FirstOrDefault()?.Response;
        
        if (modelConnectorResponse == null)
        {
            _logger.LogError($"Pipeline Execution Failed: Model Stage Error: No response from model connector {floomRequest.pipelineId}");
            
            pipelineContext.Status = PipelineExecutionStatus.Completed;

            return new FloomPipelineErrorResponse
            {
                success = false,
                message = "No response from model connector",
                statusCode = HttpStatusCode.BadRequest
            };
        }
        
        if (modelConnectorResponse.Success == false)
        {
            _logger.LogError($"Pipeline Execution Failed: Model Stage Error: No response from model connector {floomRequest.pipelineId}");
            
            pipelineContext.Status = PipelineExecutionStatus.Completed;

            return new FloomPipelineErrorResponse
            {
                success = false,
                message = modelConnectorResponse.Message,
                errorCode = modelConnectorResponse.ErrorCode,
                statusCode = HttpStatusCode.BadRequest
            };
        }
        
        pipelineContext.CurrentStage = PipelineExecutionStage.Response;

        // handle response stage

        await _responseStageHandler.ExecuteAsync(pipelineContext);
        
        // Get Response Stage Event
        var responseStageResultEvents = pipelineContext.GetEvents().OfType<ResponseStageResultEvent>();
        
        var responseStageResult = responseStageResultEvents.FirstOrDefault()?.ResultData;

        pipelineContext.Status = PipelineExecutionStatus.Completed;

        if (responseStageResult != null)
        {
            _logger.LogInformation($"Completing Pipeline Execution: {floomRequest.pipelineId}");
            
            return new FloomPipelineResponse
            {
                success = true,
                value = responseStageResult.value,
            };
        }

        _logger.LogError($"Pipeline ({floomRequest.pipelineId}) Execution Failed: Response Stage Error: No response from response formatter ");
        
        return new FloomPipelineErrorResponse
        {
            success = false,
            message = "No response from response formatter",
            statusCode = HttpStatusCode.BadRequest
        };

    }
}