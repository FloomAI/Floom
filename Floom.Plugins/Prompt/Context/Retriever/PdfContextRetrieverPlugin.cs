using Floom.Plugin.Base;

namespace Floom.Plugins.Prompt.Context.Retriever;

[FloomPlugin("floom/prompt/context/pdf")]
public class PdfContextRetrieverPlugin : ContextRetrieverPluginBase
{
    public PdfContextRetrieverPlugin()
    {
    }

    public override string GetDocumentType()
    {
        return ".pdf";
    }
}