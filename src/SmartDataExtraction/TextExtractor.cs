using Syncfusion.Licensing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.SmartDataExtractor;
using System.Text;

namespace SmartDataExtraction;

public sealed class TextExtractor
{
    private readonly string _syncfusionLicenseKey;
    private readonly IPdfService _pdfService;
    private readonly IPdfSectionSaver _sectionSaver;
    private readonly string _resultsDirectory;

    public TextExtractor(string syncfusionLicenseKey, string? resultsDirectory = null) : this(syncfusionLicenseKey, null, null, resultsDirectory)
    {
    }

    // Allow injecting a PDF service and section saver for testing/mocking and an optional results directory
    public TextExtractor(string syncfusionLicenseKey, IPdfService? pdfService = null, IPdfSectionSaver? sectionSaver = null, string? resultsDirectory = null)
    {
        _syncfusionLicenseKey = syncfusionLicenseKey;
        SyncfusionLicenseProvider.RegisterLicense(_syncfusionLicenseKey);
        _pdfService = pdfService ?? new PdfService();
        _sectionSaver = sectionSaver ?? new PdfSectionSaver();
        _resultsDirectory = !string.IsNullOrEmpty(resultsDirectory) ? resultsDirectory! : Path.Combine("data", "results");
    }

    private static void Licensing(string _syncfusionLicenseKey)
    {
        SyncfusionLicenseProvider.RegisterLicense(_syncfusionLicenseKey);
    }

    public (int pageCount, string tempPath) SplitPdfByFixedNumber(string inputPath)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file not found: {inputPath}");
        }
        var tempPath = Path.Combine(_resultsDirectory, "temp.pdf");

        var loadedDocument = new PdfLoadedDocument(inputPath);
        int pageCount = loadedDocument.Pages.Count;

        if (pageCount == 0)
        {
            loadedDocument.Close(true);

            throw new InvalidOperationException("The document has no pages.");
        }

        var splitOptions = new PdfSplitOptions
        {
            RemoveUnusedResources = true
        };

        Console.WriteLine("Creating the split options object.");
        loadedDocument.SplitByFixedNumber(tempPath, pageCount, splitOptions);
        loadedDocument.Close(true);

        return (pageCount, tempPath);
    }

    public Dictionary<int, string> FindAndValidateSections(string tempPath, List<string> pageHeaders)
    {
        var textSearchResults = _pdfService.FindText(tempPath, pageHeaders);

        var validatedResults = new Dictionary<int, string>
        {
            { 0, "Informatii_Generale" } // Add preamble section for pages before first header
        };
        var results = textSearchResults.Count;

        for (int i = 0; i < results; i++)
        {
            var section = textSearchResults.Values.ElementAt(i)[0];
            var headerIndex = pageHeaders.IndexOf(section);

            if (headerIndex == i)
            {
                validatedResults[textSearchResults.Keys.ElementAt(i)] = section;
            }
            else
            {
                var badKey = textSearchResults.Keys.ElementAt(i);
                textSearchResults.Remove(badKey);
                Console.WriteLine($"Warning: Found section '{section}' at page {badKey + 1} does not match expected header index {i}.");
                i--;
                results--;
            }
        }

        return validatedResults;
    }

    public List<string> SplitPdfBySections(string tempPath, Dictionary<int, string> validatedSections, int pageCount)
    {
        var loadedDocument = new PdfLoadedDocument(tempPath);
        if (File.Exists(tempPath)) File.Delete(tempPath);
        if (File.Exists(tempPath)) Console.WriteLine($"Warning: Temporary file still exists after deletion attempt: {tempPath}");

        var results = validatedSections.Count;
        var sectionList = new List<string>();

        //const string preamble = "Informatii_Generale";

        //// Handle preamble (pages before first section)
        //if (results > 0)
        //{
        //    var firstSectionPage = validatedSections.Keys.First();
        //    if (firstSectionPage > 0)
        //    {
        //        SavePdfSection(loadedDocument, 0, firstSectionPage - 1, preamble);
        //        Console.WriteLine($"Section: {preamble}, Start Page: 1, End Page: {firstSectionPage}");
        //    }
        //}

        // Handle each section
        var keys = validatedSections.Keys.ToList();
        for (int i = 0; i < results; i++)
        {
            var section = validatedSections[keys[i]];
            var sectionTitle = NormalizeSectionTitle(section);
            var startIndex = keys[i];
            var endIndex = (i < results - 1) ? keys[i + 1] - 1  : pageCount - 1;

            if (startIndex <= endIndex)
            {
                _sectionSaver.SavePdfSection(loadedDocument, startIndex, endIndex, sectionTitle, _resultsDirectory);
                Console.WriteLine($"Section: {section}, Start Page: {startIndex + 1}, End Page: {endIndex + 1}");
            }

            sectionList.Add(sectionTitle);
        }

        loadedDocument.Close(true);
        return sectionList;
    }

    public async Task ExtractAndSaveDataFromSectionAsync(List<string> sectionList, double confidenceThreshold)
    {
        foreach (var sectionTitle in sectionList)
        {
            var path = Path.Combine(_resultsDirectory, sectionTitle + ".pdf");

            if (!File.Exists(path))
            {
                Console.WriteLine($"Warning: Section file not found: {path}");
                continue;
            }

            try
            {
                using FileStream inputStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                var extractor = new DataExtractor
                {
                    EnableFormDetection = true,
                    EnableTableDetection = true,
                    ConfidenceThreshold = confidenceThreshold
                };
                Console.WriteLine($"Extracting data for section: {sectionTitle}");
                var jsonString = await extractor.ExtractDataAsJsonAsync(inputStream);
                Console.WriteLine($"Extracted data for section: {sectionTitle}");
                await File.WriteAllTextAsync(
                   Path.Combine(_resultsDirectory, $"{sectionTitle}.json"),
                   jsonString,
                   Encoding.UTF8);
                Console.WriteLine($"Saved JSON for section: {sectionTitle}");
                var markdown = JsonToMarkdownConverter.Convert(jsonString);
                await File.WriteAllTextAsync(
                    Path.Combine(_resultsDirectory, $"{sectionTitle}.md"),
                    markdown,
                    Encoding.UTF8);
                Console.WriteLine($"Saved Markdown for section: {sectionTitle}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting data from section '{sectionTitle}': {ex.Message}");
            }
        }
    }

    public async Task<string> ExtractDataFromSectionsAsync(List<string> sectionList, double confidenceThreshold)
    {
        var extractedData = new StringBuilder();

        foreach (var section in sectionList)
        {
            var sectionTitle = NormalizeSectionTitle(section);
            var path = Path.Combine(_resultsDirectory, sectionTitle + ".pdf");

            if (!File.Exists(path))
            {
                Console.WriteLine($"Warning: Section file not found: {path}");
                continue;
            }

            try
            {
                using FileStream inputStream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

                var extractor = new DataExtractor
                {
                    EnableFormDetection = true,
                    EnableTableDetection = true,
                    ConfidenceThreshold = confidenceThreshold
                };

                var jsonString = await extractor.ExtractDataAsJsonAsync(inputStream);
                Console.WriteLine($"Extracted data for section: {section}");
                extractedData.AppendLine($"\n{jsonString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting data from section '{section}': {ex.Message}");
            }
        }

        return extractedData.ToString();
    }



    private static string NormalizeSectionTitle(string section)
    {
        return section.Replace(" ", "_").Replace("/", "").Trim();
    }
}
