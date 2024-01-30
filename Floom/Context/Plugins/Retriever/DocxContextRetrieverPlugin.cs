using Floom.Plugin;

namespace Floom.Context;

[FloomPlugin("floom/prompt/context/docx")]
public class DocxContextRetrieverPlugin : ContextRetrieverPluginBase
{
    public DocxContextRetrieverPlugin()
    {
    }

    public override string GetDocumentType()
    {
        return ".docx";
    }
}