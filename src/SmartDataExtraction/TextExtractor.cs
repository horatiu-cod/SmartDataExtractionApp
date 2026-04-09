using Syncfusion.Licensing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Parsing;
using Syncfusion.SmartDataExtractor;
using System.Text;

namespace SmartDataExtraction;

public sealed class TextExtractor
{
    private readonly string _syncfusionLicenseKey;

    public TextExtractor(string syncfusionLicenseKey)
    {
        _syncfusionLicenseKey = syncfusionLicenseKey;
    }

    public static void Licensing(string syncfusionLicenseKey)
    {
        SyncfusionLicenseProvider.RegisterLicense(syncfusionLicenseKey);
    }

    public (int pageCount, string outputPath) SplitPdfByFixedNumber(string inputPath, string outputPath)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException($"Input file not found: {inputPath}");
        }

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
        loadedDocument.SplitByFixedNumber(outputPath, pageCount, splitOptions);
        loadedDocument.Close(true);

        return (pageCount, outputPath);
    }

    public Dictionary<int, string> FindAndValidateSections(string outputPath, List<string> pageHeaders)
    {
        var loadedDocument = new PdfLoadedDocument(outputPath);

        loadedDocument.FindText(pageHeaders, TextSearchOptions.CaseSensitive, out TextSearchResultCollection textSearchResults);

        var validatedResults = new Dictionary<int, string>();
        validatedResults.Add(0, "Informatii_Generale"); // Add preamble section for pages before first header
        var results = textSearchResults.Count;

        for (int i = 0; i < results; i++)
        {
            var section = textSearchResults.Values.ElementAt(i)[0].Text;
            var headerIndex = pageHeaders.IndexOf(section);

            if (headerIndex == i)
            {
                validatedResults[textSearchResults.Keys.ElementAt(i)] = section;
            }
            else
            {
                textSearchResults.Remove(textSearchResults.Keys.ElementAt(i));
                Console.WriteLine($"Warning: Found section '{section}' at page {textSearchResults.Keys.ElementAt(i) + 1} does not match expected header index {i}.");
                i--;
                results--;
            }
        }

        loadedDocument.Close(true);
        return validatedResults;
    }

    public List<string> SplitPdfBySections(string outputPath, Dictionary<int, string> validatedSections, int pageCount)
    {
        var loadedDocument = new PdfLoadedDocument(outputPath);
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
                SavePdfSection(loadedDocument, startIndex, endIndex, sectionTitle);
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
            var path = Path.Combine("data", "results", sectionTitle + ".pdf");

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
                Console.WriteLine($"Extracted data for section: {sectionTitle}");
                var converter = new JsonToMarkdownConverter();
                var markdown = converter.Convert(jsonString);
                await File.WriteAllTextAsync(
                    Path.Combine("data", "results", $"{sectionTitle}.md"),
                    markdown,
                    Encoding.UTF8);
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
            var path = Path.Combine("data", "results", sectionTitle + ".pdf");

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

    private void SavePdfSection(PdfLoadedDocument sourceDocument, int startPage, int endPage, string sectionTitle)
    {
        var resultDirectory = Path.Combine("data", "results");
        Directory.CreateDirectory(resultDirectory);

        var document = new PdfDocument();
        document.ImportPageRange(sourceDocument, startPage, endPage);

        var outputPath = Path.Combine(resultDirectory, sectionTitle + ".pdf");
        document.Save(outputPath);
        document.Close(true);
    }

    private string NormalizeSectionTitle(string section)
    {
        return section.Replace(" ", "_").Replace("/", "").Trim();
    }
}
