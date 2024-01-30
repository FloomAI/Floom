using Floom.Base;
using Floom.Entities.Prompt;
using Floom.Models;
using Floom.Repository;

namespace Floom.Services;

public interface IPromptsService
{
    Task<IEnumerable<PromptDtoV1>> GetAll();
    Task<PromptModel?> GetById(string id);
    Task Insert(PromptDtoV1 dto);
    Task Apply(PromptDtoV1 dto);
}

public class PromptsService : ServiceBase, IPromptsService
{
    private readonly IRepository<PromptEntity> _repository;

    public PromptsService(IRepositoryFactory repositoryFactory, IServiceProvider serviceProvider) : base(
        serviceProvider)
    {
        _repository = repositoryFactory.Create<PromptEntity>("prompts");
    }

    public async Task<IEnumerable<PromptDtoV1>> GetAll()
    {
        _logger.LogInformation("GetAll");
        var models = await _repository.GetAll();
        var dtos = models.Select(PromptDtoV1.FromEntity);
        return dtos;
    }

    public async Task<PromptModel?> GetById(string id)
    {
        var model = await _repository.Get(id, "name");
        return model == null ? null : PromptModel.FromEntity(model);
    }

    public async Task Insert(PromptDtoV1 dto)
    {
        await _repository.DeleteByName(dto.id);
        await _repository.Insert(dto.ToEntity());
    }

    public async Task Apply(PromptDtoV1 dto)
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