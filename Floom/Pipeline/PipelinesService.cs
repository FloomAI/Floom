using Floom.Base;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Floom.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Pipeline;

public interface IPipelinesService
{
    Task<IEnumerable<PipelineDtoV1>> GetAll();
    Task<PipelineDtoV1?> GetById(string id);
    Task<IActionResult?> Commit(PipelineDto pipelineDto);
    Task<IActionResult> Run(FloomRequest floomRequest);
    Task<IActionResult?> Apply(ApplyPipelineDtoV1 applyPipelineDto);
}

public class PipelinesService : ServiceBase, IPipelinesService
{
    private readonly IRepository<OldPipelineEntity> _repository;
    private readonly Lazy<IApplyPipelineUseCase> _applyPipelineUseCase;
    private readonly Lazy<IRunPipelineUseCase> _runPipelineUseCase;
    private readonly Lazy<IPipelineCommitter> _pipelineCommitter;
    private readonly Lazy<IPipelineExecutor> _pipelineExecutor;
    
    public PipelinesService(
        IRepositoryFactory repositoryFactory,
        Lazy<IApplyPipelineUseCase> applyPipelineUseCase,
        Lazy<IRunPipelineUseCase> runPipelineUseCase,
        Lazy<IPipelineCommitter> pipelineCommitter,
        Lazy<IPipelineExecutor> pipelineExecutor,
        IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _repository = repositoryFactory.Create<OldPipelineEntity>("pipelines");
        _applyPipelineUseCase = applyPipelineUseCase;
        _runPipelineUseCase = runPipelineUseCase;
        _pipelineCommitter = pipelineCommitter;
        _pipelineExecutor = pipelineExecutor;
    }

    public async Task<IEnumerable<PipelineDtoV1>> GetAll()
    {
        _logger.LogInformation("GetAll");
        var models = await _repository.GetAll();
        var dtos = models.Select(PipelineDtoV1.FromEntity);
        return dtos;
    }

    public async Task<PipelineDtoV1?> GetById(string id)
    {
        var model = await _repository.Get(id, "name");
        return model == null ? null : PipelineDtoV1.FromEntity(model);
    }

    public async Task<IActionResult?> Commit(PipelineDto pipelineDto)
    {
        await _pipelineCommitter.Value.Commit(pipelineDto);
        return new OkObjectResult(new { Message = $"Pipeline Committed Successfully" });
    }

    public async Task<IActionResult?> Apply(ApplyPipelineDtoV1 applyPipelineDto)
    {
        return await _applyPipelineUseCase.Value.ExecuteAsync(applyPipelineDto);
    }

    public async Task<IActionResult> Run(FloomRequest floomRequest)
    {
        return await _pipelineExecutor.Value.Execute(floomRequest);
    }
}