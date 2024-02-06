using System.Text.RegularExpressions;
using Floom.Pipeline;
using Floom.Pipeline.StageHandler.Model;
using Floom.Pipeline.StageHandler.Prompt;
using Floom.Plugin.Base;
using Floom.Plugin.Context;
using Microsoft.Extensions.Logging;

namespace Floom.Plugins.Common.BadWordsFilter;

[FloomPlugin("floom/plugins/bad-words-filter")]
public class FloomBadWordsFilterPlugin: FloomPluginBase
{
    private FloomBadWordsFilterConfig _config;

    public FloomBadWordsFilterPlugin()
    {
    }
    
    public override void Initialize(PluginContext context)
    {
        _logger.LogInformation($"Initializing {GetType()}");
        _config = new FloomBadWordsFilterConfig(context.Configuration.Configuration);
    }

    private List<(string Text, Action<string> UpdateAction)> GetTextsToSanitize(PipelineContext pipelineContext)
    {
        var textsToSanitize = new List<(string Text, Action<string> UpdateAction)>();

        if (pipelineContext.CurrentStage == PipelineExecutionStage.Prompt)
        {
            var promptEvent = pipelineContext.GetEvents()
                .OfType<PromptTemplateResultEvent>().MaxBy(e => e.Timestamp)
                ?.ResultData;

            if (promptEvent != null)
            {
                if (promptEvent.system != null)
                {
                    textsToSanitize.Add((promptEvent.system, sanitized => promptEvent.system = sanitized));
                }

                if (promptEvent.user != null)
                {
                    textsToSanitize.Add((promptEvent.user, (sanitized) => promptEvent.user = sanitized));
                }
            }
        }
        else if (pipelineContext.CurrentStage == PipelineExecutionStage.Response)
        {
            var responseEvent = pipelineContext.GetEvents()
                .OfType<ModelConnectorResultEvent>().MaxBy(e => e.Timestamp)
                ?.Response;

            if (responseEvent != null)
            {
                responseEvent.values.ForEach(value => 
                    textsToSanitize.Add((value.value, (sanitized) => value.value = sanitized)));
            }
        }

        return textsToSanitize;
    }

    public override Task<PluginResult> Execute(PluginContext pluginContext, PipelineContext pipelineContext)
    {
        _logger.LogInformation($"Executing {GetType()}: {pluginContext.Package}");

        var disallowList = _config.Disallow;

        if (disallowList.Count > 0)
        {
            var textsToSanitize = GetTextsToSanitize(pipelineContext);

            textsToSanitize.ForEach(textPair =>
            {
                var sanitizedText = textPair.Text;
                foreach (var item in disallowList)
                {
                    var pattern = GetRegexPattern(item);
                    var regex = new Regex(pattern, RegexOptions.Compiled);
                    sanitizedText = regex.Replace(sanitizedText, "****");
                    
                    if(sanitizedText.Contains("****"))
                    {
                        _logger.LogInformation($"{GetType()} Found disallowed item: {item}, removed it from the text.");
                    }
                }
                textPair.UpdateAction(sanitizedText);
            });
        }

        return Task.FromResult(new PluginResult() { Success = true });
    }
    
    private string GetRegexPattern(string itemType)
    {
        switch (itemType.ToLower())
        {
            case "credit-card":
                // Loose regex for credit card numbers (Visa, MasterCard, Amex)
                return @"\b(?:4\d{3}|5[1-5]\d{2}|6011|3[47]\d{2})[ -]?(?:\d{4}[ -]?){2}(?:\d{2,4}[ -]?\d{1,3})?\b";
            case "pii":
                // Simple regex for Social Security numbers (SSN)
                return @"\b\d{3}-?\d{2}-?\d{4}\b";
            case "phone-numbers":
                // North American phone number format
                return @"\b(?:\d{3}-\d{3}-\d{4})\b";
            default:
                // custom regex passed via plugin configuration
                return itemType;
        }
    }
}
