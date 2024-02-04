using System.Text;
using Floom.Plugin.Base;
using Floom.Utils;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Floom.Plugins.Prompt.Context.Retriever;

[FloomPlugin("floom/prompt/context/pdf")]
public class PdfContextRetrieverPlugin : ContextRetrieverPluginBase
{
    public PdfContextRetrieverPlugin()
    {
    }
    
    public override Task<List<string>> ParseFile(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    {
        return new PdfTextExtractor().ExtractTextFromPdfAsync(fileBytes, extractionMethod, maxCharactersPerItem);
    }
    
    protected class PdfTextExtractor
    {
        public async Task<List<string>> ExtractTextFromPdfAsync(byte[] fileBytes, ExtractionMethod extractionMethod,
            int? maxCharactersPerItem)
        {
            StringBuilder fullContent = new StringBuilder();
            var contentSplitByPages = new List<string>();

            using (PdfDocument document = PdfDocument.Open(fileBytes))
            {
                foreach (Page page in document.GetPages())
                {
                    fullContent.Append(TruncateText(page.Text + " ", maxCharactersPerItem));
                    contentSplitByPages.Add(TruncateText(page.Text + " ", maxCharactersPerItem));
                }
            }

            if (extractionMethod == ExtractionMethod.ByPages)
            {
                return contentSplitByPages;
            }

            if (extractionMethod == ExtractionMethod.ByParagraphs)
            {
                var splitByParagraphs = fullContent.ToString()
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                return splitByParagraphs;
            }

            return contentSplitByPages;
        }

        private string TruncateText(string text, int? maxLength)
        {
            if (maxLength == null)
            {
                return text;
            }

            return text.Length <= maxLength ? text : text.Substring(0, (int)maxLength);
        }
    }
}