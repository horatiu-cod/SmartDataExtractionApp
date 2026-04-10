using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

namespace SmartDataExtraction;

public interface IPdfSectionSaver
{
    void SavePdfSection(PdfLoadedDocument sourceDocument, int startPage, int endPage, string sectionTitle, string resultsDirectory);
}

public class PdfSectionSaver : IPdfSectionSaver
{
    public void SavePdfSection(PdfLoadedDocument sourceDocument, int startPage, int endPage, string sectionTitle, string resultsDirectory)
    {
        Directory.CreateDirectory(resultsDirectory);

        var document = new PdfDocument();
        document.ImportPageRange(sourceDocument, startPage, endPage);

        var outputPath = Path.Combine(resultsDirectory, sectionTitle + ".pdf");
        document.Save(outputPath);
        document.Close(true);
    }
}
