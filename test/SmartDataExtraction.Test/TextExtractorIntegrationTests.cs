using Syncfusion.Pdf;

namespace SmartDataExtraction.Test;

public class TextExtractorIntegrationTests
{
    [Fact]
    [Trait("Category","IntegrationTest")]
    public void SplitPdfBySections_Integration_WritesSectionFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "sdetest_int_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var inputPath = Path.Combine(tempDir, "input_sections.pdf");
        int pages = 4;
        using (var doc = new PdfDocument())
        {
            for (int i = 0; i < pages; i++) doc.Pages.Add();
            doc.Save(inputPath);
        }

        var resultsDir = Path.Combine(tempDir, "results");
        try
        {
            var validated = new Dictionary<int, string> { { 0, "One" }, { 2, "Two" } };
            var extractor = new SmartDataExtraction.TextExtractor("", null, new SmartDataExtraction.PdfSectionSaver(), resultsDir);
            var sections = extractor.SplitPdfBySections(inputPath, validated, pages);

            Assert.Equal(new List<string> { "One", "Two" }.ConvertAll(s => s.Replace(" ", "_").Replace("/", "")), sections);

            foreach (var title in sections)
            {
                var file = Path.Combine(resultsDir, title + ".pdf");
                Assert.True(File.Exists(file), $"Expected section file missing: {file}");
            }
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
        }
    }
}
