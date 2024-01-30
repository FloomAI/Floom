using Floom.Data;
using Floom.Data.Entities;
using Floom.Entities.Model;
using Floom.Pipeline.Entities;
using Floom.Repository;
using Floom.Services;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Pipeline;

public interface IGetPipelineUseCase
{
    public Task<IActionResult> ExecuteAsync(string pipelineId);
}

public class GetPipelineUseCase : IGetPipelineUseCase
{
    private readonly ILogger<GetPipelineUseCase> _logger;
    private readonly IRepository<OldPipelineEntity> _repository;
    private readonly IModelsService _modelsService;
    private readonly IPromptsService _promptsService;
    private readonly IResponsesService _responsesService;
    private readonly IDataService _dataService;

    public GetPipelineUseCase(
        IRepositoryFactory repositoryFactory,
        IModelsService modelsService,
        IPromptsService promptsService,
        IResponsesService responsesService,
        IDataService dataService,
        ILogger<GetPipelineUseCase> logger)
    {
        _repository = repositoryFactory.Create<OldPipelineEntity>("pipelines");
        _modelsService = modelsService;
        _dataService = dataService;
        _promptsService = promptsService;
        _responsesService = responsesService;
        _logger = logger;
    }

    public async Task<IActionResult> ExecuteAsync(string pipelineId)
    {
        var pipelineEntity = await _repository.Get(pipelineId, "name");
        if (pipelineEntity == null)
        {
            return new BadRequestObjectResult(new { Message = $"Pipeline '{pipelineId}' not found." });
        }

        var pipelineModel = new OldPipelineModel()
        {
            Id = pipelineEntity.Id.ToString(),
            name = pipelineEntity.name,
            ChatHistory = pipelineEntity.chatHistory,
            Data = new List<DataModel>()
        };

        #region Get Model

        if (pipelineEntity.models != null)
        {
            foreach (var modelStr in pipelineEntity.models)
            {
                var model = await _modelsService.GetById(modelStr);
                if (model == null)
                {
                    _logger.LogInformation($"Pipeline's Model '{model}' not found.");
                }
                else
                {
                    pipelineModel.Models ??= new List<Floom.Entities.Model.Model>();
                    pipelineModel.Models.Add(model);
                }
            }

            if (pipelineModel.Models == null)
            {
                _logger.LogInformation($"Pipeline doesn't have any models.");
            }
        }

        #endregion


        #region Get Prompt

        if (pipelineEntity.prompt != string.Empty)
        {
            var prompt = await _promptsService.GetById(pipelineEntity.prompt);

            if (prompt != null)
            {
                pipelineModel.Prompt = prompt;
            }
        }

        #endregion

        #region Get Response

        if (pipelineEntity.prompt != string.Empty)
        {
            var response = await _responsesService.GetById(pipelineEntity.response);
            if (response != null)
            {
                pipelineModel.Response = response;
            }
        }

        #endregion

        #region Get Data

        if (pipelineEntity.data != null && pipelineEntity.data.Count != 0)
        {
            foreach (string dataRef in pipelineEntity.data)
            {
                var data = await _dataService.GetDataById(dataRef);
                if (data != null)
                {
                    pipelineModel.Data ??= new List<DataModel>();
                    pipelineModel.Data.Add(data);
                }
            }
        }

        #endregion

        return new OkObjectResult(pipelineModel);
    }
}