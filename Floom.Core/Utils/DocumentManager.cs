using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Floom.Utils;

public enum ExtractionMethod
{
    ByTOC,
    ByPages,
    ByParagraphs,
}