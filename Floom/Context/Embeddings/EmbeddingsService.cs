using Floom.Base;
using Floom.Embeddings.Entities;
using Floom.Entities.Embeddings;
using Floom.Entities.Model;
using Floom.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Embeddings;

public interface IEmbeddingsService
{
    EmbeddingsModel CreateEmbeddingsEntityFromModelDto(ModelDtoV1 modelDto);
    Task<IEnumerable<EmbeddingsModel>> GetAll();
    Task<EmbeddingsModel?> GetById(string id);
    Task<IActionResult> Apply(EmbeddingsDtoV1 dto);
    Task<IActionResult> Insert(EmbeddingsModel model);
    Task<IActionResult> Validate(EmbeddingsModel embeddingsModel);
}

public class EmbeddingsService : ServiceBase, IEmbeddingsService
{
    private readonly IRepository<EmbeddingsEntity> _repository;
    // private readonly Lazy<IEmbeddingsFactory> _embeddingsVendorFactory;

    public EmbeddingsService(IRepositoryFactory repositoryFactory,
        IServiceProvider serviceProvider) : base(
        serviceProvider)
    {
        _repository = repositoryFactory.Create<EmbeddingsEntity>("embeddings");
    }

    /**
     * For use in Pipeline Apply flow, where we need to create an EmbeddingsEntity from a Pipelines Model (in case no EmbeddingsEntity exists in pipeline)
     */
    public EmbeddingsModel CreateEmbeddingsEntityFromModelDto(ModelDtoV1 modelDto)
    {
        var embeddingsModel = new EmbeddingsModel()
        {
            vendor = modelDto.vendor,
            apiKey = modelDto.apiKey
        };
        // var embeddingsProvider = _embeddingsVendorFactory.Value.Create(modelDto.vendor, modelDto.apiKey);
        // embeddingsModel.model = embeddingsProvider.GetModelName();
        // embeddingsModel.name = embeddingsModel.vendor.ToString().ToLower() + "-" + embeddingsModel.model;
        return embeddingsModel;
    }

    public async Task<IEnumerable<EmbeddingsModel>> GetAll()
    {
        _logger.LogInformation("GetAll");
        var models = await _repository.GetAll();
        var dtos = models.Select(EmbeddingsModel.FromEntity);
        return dtos;
    }

    public async Task<EmbeddingsModel?> GetById(string id)
    {
        var entity = await _repository.Get(id, "name");
        return entity == null ? null : EmbeddingsModel.FromEntity(entity);
    }

    public async Task<IActionResult> Apply(EmbeddingsDtoV1 dto)
    {
        var model = dto.ToModel();

        var result = await Validate(model);

        if (result is BadRequestObjectResult)
            return result;

        var insertResult = await Insert(dto.ToModel());

        if (insertResult is BadRequestObjectResult)
        {
            return new BadRequestObjectResult(new { Message = "Embeddings Insert Failed" });
        }

        return new OkObjectResult(new { Message = "Embeddings Apply" });
    }

    public async Task<IActionResult> Insert(EmbeddingsModel model)
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

        return new OkObjectResult(new { Message = "Embeddings Insert" });
    }

    public async Task<IActionResult> Validate(EmbeddingsModel embeddingsModel)
    {
        // var embeddingsProvider =
            // _embeddingsVendorFactory.Value.Create(embeddingsModel.vendor, embeddingsModel.apiKey,
                // embeddingsModel.model);
        // var result = await embeddingsProvider.ValidateModelAsync();
        // return result;

        return null;
    }
}