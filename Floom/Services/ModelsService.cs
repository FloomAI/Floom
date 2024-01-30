using Floom.Base;
using Floom.Entities.Model;
using Floom.LLMs;
using Floom.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Services;

public interface IModelsService
{
    Task<IEnumerable<ModelDtoV1>> GetAll();
    Task<Entities.Model.Model?> GetById(string id);
    Task Apply(params ModelDtoV1[] dtos);
    Task<IActionResult> Validate(ModelDtoV1 modelDto);
}

public class ModelsService : ServiceBase, IModelsService
{
    private readonly IRepository<ModelEntity> _repository;
    private readonly Lazy<ILLMFactory> _aiProviderFactory;

    public ModelsService(
        IRepositoryFactory repositoryFactory,
        Lazy<ILLMFactory> aiProviderFactory,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _repository = repositoryFactory.Create<ModelEntity>("models");
        _aiProviderFactory = aiProviderFactory;
    }

    public async Task<IEnumerable<ModelDtoV1>> GetAll()
    {
        _logger.LogInformation("GetAll");
        var models = await _repository.GetAll();
        var modelsDtos = models.Select(ModelDtoV1.FromModel);
        return modelsDtos;
    }

    public async Task<Entities.Model.Model?> GetById(string id)
    {
        var model = await _repository.Get(id, "name");
        return model == null ? null : Entities.Model.Model.FromEntity(model);
    }

    public async Task Insert(ModelDtoV1 modelDto)
    {
        await _repository.DeleteByName(modelDto.id);
        await _repository.Insert(modelDto.ToEntity());
    }

    public async Task Apply(params ModelDtoV1[] dtos)
    {
        foreach (var dto in dtos)
        {
            var existingItem = await _repository.Get(dto.id, "name");
            if (existingItem == null)
            {
                await _repository.Insert(dto.ToEntity());
            }
            else
            {
                await _repository.UpsertDto(dto);
            }
        }
    }

    public async Task<IActionResult> Validate(ModelDtoV1 modelDto)
    {
        var llmProvider = _aiProviderFactory.Value.Create(modelDto.vendor);
        llmProvider.SetApiKey(modelDto.apiKey);
        var result = await llmProvider.ValidateModelAsync(modelDto.model);
        return result;
    }
}