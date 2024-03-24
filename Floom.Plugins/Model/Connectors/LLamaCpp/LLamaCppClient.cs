using System.Diagnostics;
using System.Text;
using Floom.Model;
using Floom.Pipeline.Entities.Dtos;
using Floom.Pipeline.Stages.Prompt;
using LLama;
using LLama.Common;
using Microsoft.AspNetCore.Mvc;

namespace Floom.Plugins.Model.Connectors.LLamaCpp;

public class LLamaCppClient : IModelConnectorClient
{ 
    public async Task<ModelConnectorResult> GenerateTextAsync(FloomRequest promptRequest, string modelName)
    {
        var modelConnectorResult = new ModelConnectorResult();
        
        var modelPath = "Assets/orca_mini_3b--Q4_0.gguf";

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 1024,
            Seed = 1337,
            GpuLayerCount = 5
        };

        using var model = LLamaWeights.LoadFromFile(parameters);

        using var context = model.CreateContext(parameters);
        var ex = new InstructExecutor(context);

        var inferenceParams = new InferenceParams()
        {
            MaxTokens = 1024,
            TopK = 50,
            TopP = 0.5f,
            RepeatPenalty = 1.1f,
            Temperature = 0.7f,
            Mirostat = MirostatType.Mirostat2,
            MirostatTau = 10
        };

        var system_message = "";

        var fullText = new StringBuilder();

        await foreach (var text in ex.InferAsync(promptRequest.Prompt.UserPrompt, inferenceParams))
        {
            fullText.Append(text);
            Console.Write(text);
        }

        modelConnectorResult.Success = true;
        modelConnectorResult.Data = new ResponseValue()
        {
            type = DataType.String,
            value = fullText.ToString()
        };
        
        return modelConnectorResult;
    }

    public Task<IActionResult> ValidateModelAsync(string model)
    {
        return Task.FromResult<IActionResult>(new OkObjectResult(new { Message = $"Model valid" }));
    }
}