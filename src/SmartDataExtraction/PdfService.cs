using Syncfusion.Pdf.Parsing;

namespace SmartDataExtraction;

public interface IPdfService
{
    // Returns a mapping from page index to found header texts (in the order returned by the search)
    Dictionary<int, string[]> FindText(string pdfPath, List<string> pageHeaders);
}

public class PdfService : IPdfService
{
    public Dictionary<int, string[]> FindText(string pdfPath, List<string> pageHeaders)
    {
        var result = new Dictionary<int, string[]>();
        using var loadedDocument = new PdfLoadedDocument(pdfPath);
        loadedDocument.FindText(pageHeaders, TextSearchOptions.CaseSensitive, out TextSearchResultCollection textSearchResults);

        // Preserve the ordering returned by the Syncfusion search
        for (int i = 0; i < textSearchResults.Count; i++)
        {
            var key = textSearchResults.Keys.ElementAt(i);
            var hits = textSearchResults.Values.ElementAt(i);
            var texts = hits.Select(h => h.Text).ToArray();
            result.Add(key, texts);
        }

        loadedDocument.Close(true);
        return result;
    }
}
