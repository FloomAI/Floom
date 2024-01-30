using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

//using PdfSharp.Pdf;
//using PdfSharp.Pdf.Content;
//using PdfSharp.Pdf.Content.Objects;
//using PdfSharp.Pdf.IO;

using Xceed.Document.NET;
using Xceed.Words.NET;

//using iText.Kernel.Pdf;
//using iText.Kernel.Pdf.Canvas.Parser;
//using iText.Kernel.Pdf.Canvas.Parser.Listener;
//using iText.Kernel.Pdf.Canvas.Parser.Filter;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using iText.Kernel.Pdf.Canvas.Parser.Listener.Filter;

//using Spire.Doc;
//using Spire.Doc.Documents;

public enum ExtractionMethod
{
    ByTOC,
    ByPages,
    ByParagraphs
}

public class DocumentManager
{
    public async Task<List<string>> ExtractTextAsync(string extension, byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    {
        switch (extension.ToLower()) // Replace "fileName" with the actual file name or path
        {
            case ".pdf":
                return await ExtractTextFromPdfAsync(fileBytes, extractionMethod, maxCharactersPerItem);
            case ".docx":
                return await ExtractTextFromDocxAsync(fileBytes, extractionMethod, maxCharactersPerItem);
            default:
                throw new NotSupportedException("Unsupported file format.");
        }
    }

    private async Task<List<string>> ExtractTextFromPdfAsync(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
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
            var splitByParagraphs = fullContent.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            return splitByParagraphs;
        }

        return contentSplitByPages;
    }


    //private async Task<List<string>> ExtractTextFromPdfAsync(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    //{
    //    var paragraphs = new List<string>();

    //    using (var memoryStream = new MemoryStream(fileBytes))
    //    {
    //        var pdfDocument = PdfReader.Open(memoryStream);

    //        foreach (var page in pdfDocument.Pages)
    //        {
    //            var pageContent = ContentReader.ReadContent(page);
    //            //var text = ContentReader.ExtractText(page);

    //            //pageContent
    //            //.Contents.Elements.GetDictionary(0).Stream.ToString();

    //            var pageText = ExtractTextFromPdfContent(pageContent);
    //            //var pageText = ExtractTextFromPdf(page);
    //            //PdfSharpExtensions



    //            if (extractionMethod == ExtractionMethod.ByPages)
    //            {
    //                if (maxCharactersPerItem.HasValue)
    //                {
    //                    pageText = TruncateText(pageText, maxCharactersPerItem.Value);
    //                }
    //                paragraphs.Add(pageText);
    //            }

    //            else if (extractionMethod == ExtractionMethod.ByParagraphs)
    //            {
    //                var pageParagraphs = pageText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    //                foreach (var paragraph in pageParagraphs)
    //                {
    //                    var truncatedParagraph = paragraph;
    //                    if (maxCharactersPerItem.HasValue)
    //                    {
    //                        truncatedParagraph = TruncateText(paragraph, maxCharactersPerItem.Value);
    //                    }
    //                    paragraphs.Add(truncatedParagraph);
    //                }
    //            }
    //        }
    //    }

    //    return paragraphs;
    //}

    //private async Task<List<string>> ExtractTextFromDocxAsync(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
    //{
    //    var paragraphs = new List<string>();

    //    using (var memoryStream = new MemoryStream(fileBytes))
    //    {
    //        var doc = new Spire.Doc.Document(memoryStream);
    //        //doc.TOC.
    //        foreach (var section in doc.Sections)
    //        {
    //            if (extractionMethod == ExtractionMethod.BySections)
    //            {
    //                // Not applicable for DOCX, skip this part
    //            }

    //            foreach (var paragraph in section.Paragraphs)
    //            {
    //                var paragraphText = paragraph.Text;

    //                paragraphText = TruncateText(paragraphText, maxCharactersPerItem);

    //                paragraphs.Add(paragraphText);
    //            }
    //        }
    //    }

    //    return paragraphs;
    //}



    //private string ExtractTextFromPdf(PdfPage page)
    //{
    //    var textBuilder = new StringBuilder();

    //    // Extract text from each element on the page
    //    foreach (var element in ContentReader.ReadContent(page))
    //    {
    //        if (element is PdfSharp.Pdf.Content.Cjk.CjkTextBox cjkTextBox)
    //        {
    //            // For CJK (Chinese, Japanese, Korean) text boxes
    //            textBuilder.AppendLine(cjkTextBox.Text);
    //        }
    //        else if (element is PdfSharp.Pdf.Content.Text.TextObject textObject)
    //        {
    //            // For regular text objects
    //            textBuilder.AppendLine(textObject.Text);
    //        }
    //        // Add additional conditions for other types of elements if needed.
    //    }

    //    return textBuilder.ToString();
    //}

    private async Task<List<string>> ExtractTextFromDocxAsync(byte[] fileBytes, ExtractionMethod extractionMethod, int? maxCharactersPerItem)
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

    private string ExtractTextFromSection(Section section, int? maxCharactersPerItem)
    {
        var textItems = new List<string>();

        foreach (var paragraph in section.Paragraphs)
        {
            var paragraphText = ExtractTextFromParagraph(paragraph, maxCharactersPerItem);
            textItems.Add(paragraphText);
        }

        return string.Join(" ", textItems);
    }

    //private string ExtractTextFromPage(Section section, int pageNumber, int? maxCharactersPerItem)
    //{
    //    var page = section.PageNumber(pageNumber);
    //    var textItems = new List<string>();

    //    foreach (var paragraph in page.Paragraphs)
    //    {
    //        var paragraphText = ExtractTextFromParagraph(paragraph, maxCharactersPerItem);
    //        textItems.Add(paragraphText);
    //    }

    //    return string.Join(" ", textItems);
    //}

    private string ExtractTextFromParagraph(Paragraph paragraph, int? maxCharactersPerItem)
    {
        var paragraphText = paragraph.Text;
        if (maxCharactersPerItem.HasValue && paragraphText.Length > maxCharactersPerItem.Value)
        {
            paragraphText = paragraphText.Substring(0, maxCharactersPerItem.Value);
        }

        return paragraphText;
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
