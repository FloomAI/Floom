using Floom.Plugin;

namespace Floom.Context;

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