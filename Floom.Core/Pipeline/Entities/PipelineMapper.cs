using Floom.Pipeline.Entities.Dtos;
using Floom.Plugin;
using Floom.Plugin.Base;

namespace Floom.Pipeline.Entities;

public static class PipelineMapper
{
    public static PipelineEntity ToEntity(this PipelineDto dto)
    {
        var pipelineEntity = new PipelineEntity
        {
            kind = dto.Kind,
            name = dto.Pipeline.Name
        };

        if (dto.Pipeline.Model != null)
        {
            pipelineEntity.model = dto.Pipeline.Model.Select(m => m.ToEntity()).ToList();
        }

        if (dto.Pipeline.Prompt != null)
        {
            pipelineEntity.prompt = dto.Pipeline.Prompt.ToEntity();
        }

        if (dto.Pipeline.Response != null)
        {
            pipelineEntity.response = dto.Pipeline.Response.ToEntity();
        }

        if (dto.Pipeline.Global != null)
        {
            pipelineEntity.global = dto.Pipeline.Global.Select(g => g.ToEntity()).ToList();
        }

        return pipelineEntity;
    }

    private static PluginConfigurationEntity ToEntity(this PipelineDto.PluginConfigurationDto pluginConfig)
    {
        return new PluginConfigurationEntity
        {
            package = pluginConfig.Package,
            configuration = new Dictionary<string, object>(pluginConfig.Configuration)
        };
    }
    
    public static PluginConfiguration ToModel(this PluginConfigurationEntity pluginConfigurationEntity)
    {
        return new PluginConfiguration
        {
            Package = pluginConfigurationEntity.package,
            Configuration = pluginConfigurationEntity.configuration
        };
    }

    private static PipelineEntity.PromptStageEntity ToEntity(this PipelineDto.PromptStageDto prompt)
    {
        var promptStageEntity = new PipelineEntity.PromptStageEntity();

        if (prompt.Template != null)
        {
            promptStageEntity.template = prompt.Template.ToEntity();
        }

        if (prompt.Context != null)
        {
            promptStageEntity.context = prompt.Context.Select(c => c.ToEntity()).ToList();
        }

        if (prompt.Optimization != null)
        {
            promptStageEntity.optimization = prompt.Optimization.Select(o => o.ToEntity()).ToList();
        }

        if (prompt.Validation != null)
        {
            promptStageEntity.validation = prompt.Validation.Select(v => v.ToEntity()).ToList();
        }

        return promptStageEntity;
    }

    private static PipelineEntity.ResponseStageEntity ToEntity(this PipelineDto.ResponseStageDto response)
    {
        var responseStageEntity = new PipelineEntity.ResponseStageEntity();
        if (response.Format != null)
        {
            responseStageEntity.format = response.Format.Select(f => f.ToEntity()).ToList();
        }

        if (response.Validation != null)
        {
            responseStageEntity.validation = response.Validation.Select(v => v.ToEntity()).ToList();
        }

        return responseStageEntity;
    }

    private static Pipeline.PromptStage ToModel(this PipelineEntity.PromptStageEntity promptStageEntity)
    {
        var promptStage = new Pipeline.PromptStage();
        
        if (promptStageEntity.template != null)
        {
            promptStage.Template = promptStageEntity.template.ToModel();
        }
        
        if (promptStageEntity.context != null)
        {
            promptStage.Context = promptStageEntity.context.Select(c => c.ToModel()).ToList();
        }
        
        if (promptStageEntity.optimization != null)
        {
            promptStage.Optimization = promptStageEntity.optimization.Select(o => o.ToModel()).ToList();
        }
        
        if (promptStageEntity.validation != null)
        {
            promptStage.Validation = promptStageEntity.validation.Select(v => v.ToModel()).ToList();
        }

        return promptStage;
    }
    
    private static Pipeline.ResponseStage ToModel(this PipelineEntity.ResponseStageEntity responseStageEntity)
    {
        var responseStage = new Pipeline.ResponseStage();
        
        if (responseStageEntity.format != null)
        {
            responseStage.Format = responseStageEntity.format.Select(f => f.ToModel()).ToList();
        }
        
        if (responseStageEntity.validation != null)
        {
            responseStage.Validation = responseStageEntity.validation.Select(v => v.ToModel()).ToList();
        }

        return responseStage;
    }
    
    public static Pipeline ToModel(this PipelineEntity pipelineEntity)
    {
        var pipeline = new Pipeline
        {
            Name = pipelineEntity.name,
            Kind = pipelineEntity.kind
        };
        
        if (pipelineEntity.model != null)
        {
            pipeline.Model = pipelineEntity.model.Select(m => m.ToModel()).ToList();
        }
        
        if (pipelineEntity.prompt != null)
        {
            pipeline.Prompt = pipelineEntity.prompt.ToModel();
        }
        
        if (pipelineEntity.response != null)
        {
            pipeline.Response = pipelineEntity.response.ToModel();
        }
        
        if (pipelineEntity.global != null)
        {
            pipeline.Global = pipelineEntity.global.Select(g => g.ToModel()).ToList();
        }

        return pipeline;
    }
}