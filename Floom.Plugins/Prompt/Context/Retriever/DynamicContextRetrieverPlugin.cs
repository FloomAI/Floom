using Floom.Plugin.Base;
using Floom.Plugins.Model.Connectors.OpenAi;
using Floom.Utils;

namespace Floom.Plugins.Prompt.Context.Retriever;

[FloomPlugin("floom/prompt/context/dynamic")]
public class DynamicContextRetrieverPlugin : ContextRetrieverPluginBase
{
    public override async Task<List<string>> ParseFile(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    {
        // send fileBytes to OpenAI API with given prompt
        var client = new OpenAiClient
        {
            ApiKey = "REMOVED"
        };

        // generate dummy floom request object
        
        // return empty array
        return new List<string>();
    }
}