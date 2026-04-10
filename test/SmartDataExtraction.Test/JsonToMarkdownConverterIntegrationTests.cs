using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

namespace SmartDataExtraction.Test;

public class JsonToMarkdownConverterIntegrationTests
{
    private class FakeSaver : IPdfSectionSaver
    {
        public List<(int start, int end, string title)> Calls { get; } = [];
        public void SavePdfSection(PdfLoadedDocument sourceDocument, int startPage, int endPage, string sectionTitle, string resultsDirectory)
        {
            Calls.Add((startPage, endPage, sectionTitle));
        }
    }

    [Fact]
    [Trait("Category", "IntegrationTest")]
    public void SplitPdfByFixedNumber_CreatesOutputAndReturnsCount()
    {
        // create temp pdf with 3 pages
        var tempDir = Path.Combine(Path.GetTempPath(), "sdetest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.pdf");
        int pages = 3;
        using (var doc = new PdfDocument())
        {
            for (int i = 0; i < pages; i++) doc.Pages.Add();
            doc.Save(inputPath);
        }

        var extractor = new TextExtractor("", tempDir);
        var outPath = Path.Combine(tempDir, "temp.pdf");
        var (pageCount, outputPath) = extractor.SplitPdfByFixedNumber(inputPath);

        try
        {
            Assert.Equal(pages, pageCount);
            Assert.Equal(outputPath, outPath);
        }
        finally
        {
            // cleanup
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void SplitPdfBySections_CreatesSectionFiles()
    {
        // create temp pdf with 3 pages
        var tempDir = Path.Combine(Path.GetTempPath(), "sdetest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input_sections.pdf");
        int pages = 3;
        using (var doc = new PdfDocument())
        {
            for (int i = 0; i < pages; i++) doc.Pages.Add();
            doc.Save(inputPath);
        }

        // Use a fake saver to avoid writing files; verify calls
        var fakeSaver = new FakeSaver();
        var extractor = new TextExtractor("", null, fakeSaver, tempDir);
        var validated = new Dictionary<int, string> { { 0, "Section One" }, { 2, "Section Two" } };

        try
        {
            var sections = extractor.SplitPdfBySections(inputPath, validated, pages);

            var expectedTitles = new List<string> { "Section_One", "Section_Two" };
            Assert.Equal(expectedTitles, sections);

            // fake saver should have been called twice with correct ranges and titles
            Assert.Equal(2, fakeSaver.Calls.Count);
            Assert.Contains((0, 1, "Section_One"), fakeSaver.Calls);
            Assert.Contains((2, 2, "Section_Two"), fakeSaver.Calls);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
