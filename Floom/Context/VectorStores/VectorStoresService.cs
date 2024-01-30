using Floom.Base;
using Floom.Entities.VectorStore;
using Floom.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Floom.VectorStores;

public interface IVectorStoresService
{
    Task<IEnumerable<VectorStoreModel>> GetAll();
    Task<VectorStoreModel?> GetById(string id);
    Task<VectorStoreEntity?> GetEntityById(string id);
    Task<IActionResult> Apply(VectorStoreModel model);
    Task<IActionResult> Insert(VectorStoreModel model);
    Task<IActionResult> Validate(VectorStoreModel model);
}

public class VectorStoresService : ServiceBase, IVectorStoresService
{
    private readonly IRepository<VectorStoreEntity> _repository;

    public VectorStoresService(IRepositoryFactory repositoryFactory
        , IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _repository = repositoryFactory.Create<VectorStoreEntity>("vector-stores");
    }

    public async Task<IEnumerable<VectorStoreModel>> GetAll()
    {
        _logger.LogInformation("GetAll");
        var models = await _repository.GetAll();
        var dtos = models.Select(VectorStoreModel.FromEntity);
        return dtos;
    }

    public async Task<VectorStoreModel?> GetById(string id)
    {
        var model = await _repository.Get(id, "name");
        return model == null ? null : VectorStoreModel.FromEntity(model);
    }

    public async Task<VectorStoreEntity?> GetEntityById(string id)
    {
        var model = await _repository.Get(id, "name");
        return model;
    }

    public async Task<IActionResult> Apply(VectorStoreModel model)
    {
        // var vectorStoreProvider = _vectorStoresFactory.Value.Create(model);
        // var result = await vectorStoreProvider.HealthCheck();
        // if (result is BadRequestObjectResult)
        //     return result;
        //
        // var insertResult = await Insert(model: model);
        //
        // if (insertResult is BadRequestObjectResult)
        // {
        //     return new BadRequestObjectResult(new { Message = "Vector Store Insert Failed" });
        // }

        return new OkObjectResult(new { Message = "Vector Store Applied" });
    }

    public async Task<IActionResult> Insert(VectorStoreModel model)
    {
        var entity = model.ToEntity();

        var existingItem = await _repository.Get(entity.name, "name");

        if (existingItem == null)
        {
            await _repository.Insert(entity);
        }
        else
        {
            await _repository.Update(entity);
        }

        return new OkObjectResult(new { Message = "Vector Store Insert" });
    }

    public async Task<IActionResult> Validate(VectorStoreModel model)
    {
        return new OkObjectResult(new { Message = "Vector Store Validated" });
        // var vectorStoreProvider = _vectorStoresFactory.Value.Create(model);
        // var result = await vectorStoreProvider.HealthCheck();
        // return result;
    }
}