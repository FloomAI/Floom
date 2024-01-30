using Floom.Base;
using Floom.Entities.Response;
using Floom.Models;
using Floom.Repository;

namespace Floom.Services;

public interface IResponsesService
{
    Task<IEnumerable<ResponseDtoV1>> GetAll();
    Task<ResponseModel?> GetById(string id);
    Task Insert(ResponseDtoV1 dto);
    Task Apply(ResponseDtoV1 dto);
}

public class ResponsesService : ServiceBase, IResponsesService
{
    private readonly IRepository<ResponseEntity> _repository;

    public ResponsesService(IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _repository = repositoryFactory.Create<ResponseEntity>("responses");
    }

    public async Task<IEnumerable<ResponseDtoV1>> GetAll()
    {
        _logger.LogInformation("GetAll");
        var models = await _repository.GetAll();
        var dtos = models.Select(ResponseDtoV1.FromEntity);
        return dtos;
    }

    public async Task<ResponseModel?> GetById(string id)
    {
        var model = await _repository.Get(id, "name");
        return model == null ? null : ResponseModel.FromEntity(model);
    }

    public async Task Insert(ResponseDtoV1 dto)
    {
        await _repository.DeleteByName(dto.id);
        await _repository.Insert(dto.ToEntity());
    }

    public async Task Apply(ResponseDtoV1 dto)
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