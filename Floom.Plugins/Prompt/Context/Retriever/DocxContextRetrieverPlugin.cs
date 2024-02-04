using Floom.Plugin.Base;
using Floom.Utils;
using Xceed.Document.NET;
using Xceed.Words.NET;
using Section = DocumentFormat.OpenXml.Office2010.PowerPoint.Section;

namespace Floom.Plugins.Prompt.Context.Retriever;

[FloomPlugin("floom/prompt/context/docx")]
public class DocxContextRetrieverPlugin : ContextRetrieverPluginBase
{
    public DocxContextRetrieverPlugin()
    {
    }
    
    public override Task<List<string>> ParseFile(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    {
        return new DocxTextExtractor().ExtractTextFromDocxAsync(fileBytes, extractionMethod, maxCharactersPerItem);
    }

    protected class DocxTextExtractor
    {
        public async Task<List<string>> ExtractTextFromDocxAsync(byte[] fileBytes, ExtractionMethod extractionMethod,
        int? maxCharactersPerItem)
        {
            var textItems = new List<string>();

            using (var memoryStream = new MemoryStream(fileBytes))
            {
                using (var doc = DocX.Load(memoryStream))
                {
                    if (extractionMethod == ExtractionMethod.ByPages)
                    {
                        // Split by sections (the pages of docx)
                        foreach (var section in doc.Sections)
                        {
                            var sectionText = ExtractTextFromSection(section, maxCharactersPerItem);
                            textItems.Add(sectionText);
                        }
                    }
                    else if (extractionMethod == ExtractionMethod.ByTOC)
                    {
                        // Split by TOC entries (including child items)
                        var tocEntries = ExtractTOCEntries(doc);
                        foreach (var tocEntry in tocEntries)
                        {
                            var tocText = ExtractTextFromParagraph(tocEntry, maxCharactersPerItem);
                            textItems.Add(tocText);
                        }
                    }
                    //else if (extractionMethod == ExtractionMethod.ByPages)
                    //{
                    //    // Split by pages
                    //    foreach (var section in doc.Sections)
                    //    {
                    //        var pages = section.page;
                    //        for (int pageNumber = 1; pageNumber <= pages; pageNumber++)
                    //        {
                    //            var pageText = ExtractTextFromPage(section, pageNumber, maxCharactersPerItem);
                    //            textItems.Add(pageText);
                    //        }
                    //    }
                    //}
                    else if (extractionMethod == ExtractionMethod.ByParagraphs)
                    {
                        // Split by paragraphs
                        foreach (var paragraph in doc.Paragraphs)
                        {
                            var paragraphText = ExtractTextFromParagraph(paragraph, maxCharactersPerItem);
                            textItems.Add(paragraphText);
                        }
                    }
                }
            }

            return textItems;
        }
        
        private List<Paragraph> ExtractTOCEntries(DocX doc)
        {
            // Helper method to extract all TOC entries (including nested ones)
            var tocEntries = new List<Paragraph>();

            foreach (var paragraph in doc.Paragraphs)
            {
                if (paragraph.StyleName == "TOC1" || paragraph.StyleName == "TOC2" || paragraph.StyleName == "TOC3")
                {
                    tocEntries.Add(paragraph);
                }
            }

            return tocEntries;
        }
        
        private string ExtractTextFromSection(Xceed.Document.NET.Section section, int? maxCharactersPerItem)
        {
            var textItems = new List<string>();

            foreach (var paragraph in section.Paragraphs)
            {
                var paragraphText = ExtractTextFromParagraph(paragraph, maxCharactersPerItem);
                textItems.Add(paragraphText);
            }

            return string.Join(" ", textItems);
        }

        private string ExtractTextFromParagraph(Paragraph paragraph, int? maxCharactersPerItem)
        {
            var paragraphText = paragraph.Text;
            if (maxCharactersPerItem.HasValue && paragraphText.Length > maxCharactersPerItem.Value)
            {
                paragraphText = paragraphText.Substring(0, maxCharactersPerItem.Value);
            }

            return paragraphText;
        }
    }
}