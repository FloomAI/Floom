using Floom.Plugin.Base;

namespace Floom.Plugins.Prompt.Context.Retriever;

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