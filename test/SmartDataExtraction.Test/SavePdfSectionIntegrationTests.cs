using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;

namespace SmartDataExtraction.Test;

public class SavePdfSectionIntegrationTests
{
    [Fact]
    [Trait("Category","IntegrationTest")]
    public void PdfSectionSaver_ExtractsExactPageRange()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "sdetest_save_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input.pdf");
        int pages = 5;
        using (var doc = new PdfDocument())
        {
            for (int i = 0; i < pages; i++) doc.Pages.Add();
            doc.Save(inputPath);
        }

        var resultsDir = Path.Combine(tempDir, "results");
        Directory.CreateDirectory(resultsDir);

        try
        {
            using var loaded = new PdfLoadedDocument(inputPath);
            var saver = new SmartDataExtraction.PdfSectionSaver();

            // extract pages 1..3 (0-based indices 0..2)
            saver.SavePdfSection(loaded, 0, 2, "part1", resultsDir);
            var outPath = Path.Combine(resultsDir, "part1.pdf");
            Assert.True(File.Exists(outPath));

            using var outLoaded = new PdfLoadedDocument(outPath);
            Assert.Equal(3, outLoaded.Pages.Count);

            // extract last two pages (indices 3..4)
            saver.SavePdfSection(loaded, 3, 4, "part2", resultsDir);
            var outPath2 = Path.Combine(resultsDir, "part2.pdf");
            Assert.True(File.Exists(outPath2));

            using var outLoaded2 = new PdfLoadedDocument(outPath2);
            Assert.Equal(2, outLoaded2.Pages.Count);
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
