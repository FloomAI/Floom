using Floom.Plugin;

namespace Floom.Model.OpenAi;

[FloomPlugin("floom/model/connector/openai")]
public class OpenAiModelConnectorPlugin : ModelConnectorPluginBase<OpenAiClient>
{
    protected override void InitializeClient(ModelConnectorPluginConfig settings)
    {
        _client.ApiKey = settings.ApiKey;
    }
}

// public class OpenAiModelConnectorPlugin : FloomPluginBase
// {
//     private readonly OpenAiClient _openAiClient = new();
//     private ModelConnectorPluginConfig _settings;
//     
//     public override void Initialize(PluginContext context)
//     {
//         _logger.LogInformation($"Initializing {GetType()}");
//
//         // Initialize settings with specific plugin settings class
//         _settings = new ModelConnectorPluginConfig(context.Configuration.Configuration);
//         _openAiClient.ApiKey = _settings.ApiKey;
//     }
//     
//     public override async Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
//     {
//         _logger.LogInformation($"Executing {GetType()}: {pluginContext.Package}");
//         
//         var promptTemplateResultEvent = pipelineContext.GetEvents()
//             .OfType<PromptTemplateResultEvent>()
//             .FirstOrDefault()
//             ?.ResultData;
//
//         var promptContextResultEvent = pipelineContext.GetEvents()
//             .OfType<PromptContextResultEvent>()
//             .FirstOrDefault()
//             ?.ResultData;
//
//         // Determine the final promptRequest based on the available events
//         var promptRequest = promptContextResultEvent ?? promptTemplateResultEvent;
//         
//         if (_settings.Model != null)
//         {
//             var response = await _openAiClient.GenerateTextAsync(promptRequest, _settings.Model);
//             
//             _logger.LogInformation($"{GetType()} Completed Successfully");
//             
//             return new PluginResult()
//             {
//                 Success = true,
//                 ResultData = response
//             };
//         }
//
//         _logger.LogInformation($"{GetType()} Completed With Errors");
//
//         return new PluginResult()
//         {
//             Success = false
//         };
//     }
// }