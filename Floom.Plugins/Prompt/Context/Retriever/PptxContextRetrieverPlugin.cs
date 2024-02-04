using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Floom.Plugin.Base;
using Floom.Utils;

namespace Floom.Plugins.Prompt.Context.Retriever;

[FloomPlugin("floom/prompt/context/pptx")]
public class PptxContextRetrieverPlugin : ContextRetrieverPluginBase
{
    public PptxContextRetrieverPlugin()
    {
    }
    
    public override Task<List<string>> ParseFile(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    {
        return new PptxTextExtractor().ExtractTextFromPptxAsync(fileBytes, extractionMethod, maxCharactersPerItem);
    }
    
    private class PptxTextExtractor
    {
        public async Task<List<string>> ExtractTextFromPptxAsync(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
        {
            var textItems = new List<string>();

            using (var memoryStream = new MemoryStream(fileBytes))
            {
                using (PresentationDocument presentationDocument = PresentationDocument.Open(memoryStream, false))
                {
                    var presentationPart = presentationDocument.PresentationPart;
                    if (presentationPart != null)
                    {
                        foreach (var slidePart in presentationPart.SlideParts)
                        {
                            StringBuilder slideText = new StringBuilder();
                            foreach (var text in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>())
                            {
                                slideText.Append(text.Text + " ");
                            }

                            var slideTextStr = TruncateText(slideText.ToString(), maxCharactersPerItem);

                            // Depending on the extraction method, you can further split the text
                            switch (extractionMethod)
                            {
                                case ExtractionMethod.ByPages:
                                    textItems.Add(slideTextStr);
                                    break;
                                case ExtractionMethod.ByParagraphs:
                                    // Assuming paragraphs are separated by new lines in slide text
                                    textItems.AddRange(slideTextStr.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(paragraph => TruncateText(paragraph, maxCharactersPerItem)));
                                    break;
                            }
                        }
                    }
                }
            }

            return textItems;
        }

        private string TruncateText(string text, int? maxCharactersPerItem)
        {
            // Implement truncation logic
            if (!maxCharactersPerItem.HasValue || text.Length <= maxCharactersPerItem.Value)
                return text;

            return text.Substring(0, maxCharactersPerItem.Value);
        }
    }
}
