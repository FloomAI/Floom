using System.Text;
using Floom.Plugin.Base;
using Floom.Utils;

namespace Floom.Plugins.Prompt.Context.Retriever;

[FloomPlugin("floom/prompt/context/txt")]
public class TxtContextRetrieverPlugin : ContextRetrieverPluginBase
{
    public TxtContextRetrieverPlugin()
    {
    }

    public override Task<List<string>> ParseFile(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    {
        return new TxtTextExtractor().ExtractTextFromTxtAsync(fileBytes, extractionMethod, maxCharactersPerItem);
    }
    
    protected class TxtTextExtractor
    {
        public async Task<List<string>> ExtractTextFromTxtAsync(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
        {
            var textItems = new List<string>();
            var content = Encoding.UTF8.GetString(fileBytes); // Assuming UTF-8 encoding

            // Handle the extraction based on the method
            switch (extractionMethod)
            {
                case ExtractionMethod.ByParagraphs:
                    // Split the content by new lines to extract paragraphs
                    var paragraphs = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var paragraph in paragraphs)
                    {
                        var truncatedParagraph = TruncateText(paragraph, maxCharactersPerItem);
                        textItems.Add(truncatedParagraph);
                    }
                    break;
                default:
                    // For TXT, treating any non-paragraph based method as a full content extraction
                    // This means if the method isn't by paragraphs, we just apply the character limit to the whole content
                    var truncatedContent = TruncateText(content, maxCharactersPerItem);
                    textItems.Add(truncatedContent);
                    break;
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