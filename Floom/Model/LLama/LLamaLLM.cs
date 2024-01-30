using System.Diagnostics;
using System.Text;
using Floom.Model;
using Floom.Pipeline.Entities;
using Floom.Pipeline.Entities.Dtos;
using Microsoft.AspNetCore.Mvc;
using LLama.Common;
using LLama;

namespace Floom.LLMs.LLama;

public class LLamaLLM
{
    public void SetApiKey(string apiKey)
    {
    }

    // {  tinyllama 0.3
    //     "pipelineId": "lama-pipeline",
    //     "input": "<|im_start|>system You are a software engineer assistant, provide clear python code.<|im_end|><|im_start|>user Write python function to calculate Fibonacci numbers? Include only function code<|im_end|><|im_start|>"
    // }
    // 


    // minichat-3b.q5_k_m.gguf
    // {
    //     "pipelineId": "lama-pipeline",
    //     "input": "<s> [|System|] You are an AI assistant that follows instruction extremely well. Responses should be precise and short</s><s> [|User|]\n Who was the first USA president?</s>[|Assistant|]"
    // }  
    //
    // { minichat
    //     "pipelineId": "lama-pipeline",
    //     "input": "<s> [|System|] </s><s> [|Input|]\n Write a python code to calculate Fibonacci numbers</s>[|Assistant|]"
    // }

    // mambagpt "### User:\n
    // minichat "### Prompt:\n
    // orcamini ###  Prompt:\n

    // ORCA MINI
    // ### System:\n \n\n### User:\n  Write a python code to calculate Fibonacci numbers\n\n### Response:\n 
    public async Task<PromptResponse> GenerateTextAsync(PromptRequest promptRequest, string modelName)
    {
        var promptResponse = new PromptResponse();
        var swPrompt = new Stopwatch();
        swPrompt.Start();

        var modelPath = "Assets/orca_mini_3b--Q4_0.gguf";

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 1024,
            Seed = 1337,
            GpuLayerCount = 5
        };

        using var model = LLamaWeights.LoadFromFile(parameters);

        // var ex = new StatelessExecutor(model, parameters);
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

        await foreach (var text in ex.InferAsync(promptRequest.user, inferenceParams))
        {
            fullText.Append(text);
            Console.Write(text);
        }

        promptResponse = new PromptResponse()
        {
            elapsedProcessingTime = swPrompt.ElapsedMilliseconds,
        };

        //Add all choices
        promptResponse.values.Add(
            new ResponseValue()
            {
                type = DataType.String,
                value = fullText.ToString()
            }
        );
        // Initialize a chat session
        // using var context = model.CreateContext(parameters);
        // var ex = new InteractiveExecutor(context);
        // ChatSession session = new ChatSession(ex);
        //
        // // show the prompt
        // Console.WriteLine();
        // Console.Write(prompt);
        //
        // // run the inference in a loop to chat with LLM
        // while (prompt != "stop")
        // {
        //     await foreach (var text in session.ChatAsync(prompt, new InferenceParams() { Temperature = 0.6f, AntiPrompts = new List<string> { "User:" } }))
        //     {
        //         Console.Write(text);
        //     }
        //     prompt = Console.ReadLine();
        // }

        // save the session

        return promptResponse;
    }

    public Task<IActionResult> ValidateModelAsync(string model)
    {
        return Task.FromResult<IActionResult>(new OkObjectResult(new { Message = $"Model valid" }));
    }
}