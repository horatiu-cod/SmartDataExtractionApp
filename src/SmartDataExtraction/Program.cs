using DiffPlex.DiffBuilder;
using Microsoft.Extensions.Configuration;
using SmartDataExtraction;
using System.Text;

// Load configuration from user secrets
var builder = new ConfigurationBuilder()
    .AddUserSecrets<Program>();
var configuration = builder.Build();

var key = configuration["SyncfusionLicenseKey"];
//TextExtractor.Licensing(key);

var inputPath = Path.Combine("data", "inputuri.pdf");
var resultsDirectory  = Path.Combine("data", "results");


if (!File.Exists(inputPath))
{
    Console.WriteLine("Input file not found.");
    return;
}

// Initialize TextExtractor
var extractor = new TextExtractor(key, resultsDirectory);

// Define page headers for splitting
List<string> pageHeaders =
[
    "SOLICITANT",
    "Responsabil de proiect / Persoană de contact",
    "Capacitate solicitant",
    "Localizare proiect",
    "Obiective proiect",
    "Justificare / Context / Relevanță / Oportunitate și contribuția la obiectivul specific",
    "Caracter durabil al proiectului",
    "Descriere investiție",
    "Indicatori de realizare și de rezultat (program)",
    "Indicatori suplimentari proiect",
    "PLAN DE ACHIZIȚII",
    "Resurse Umane",
    "Rezultate așteptate / Realizări așteptate",
    "Activități previzionate",
    "Indicatori de etapă",
    "Plan de monitorizare a proiectului",
    "BUGET TOTAL",
    "Criterii ETF"
];

try
{
    // Step 1: Split PDF by fixed number of pages
    Console.WriteLine("Step 1: Splitting PDF by fixed page count...");
    var (pageCount, tempOutput) = extractor.SplitPdfByFixedNumber(inputPath);

    // Step 2: Find and validate sections
    Console.WriteLine("Step 2: Finding and validating sections...");
    var validatedSections = extractor.FindAndValidateSections(tempOutput, pageHeaders);
    Console.WriteLine($"Found {validatedSections.Count} valid sections.");

    // Step 3: Split PDF by sections
    Console.WriteLine("Step 3: Splitting PDF by sections...");
    var sectionList = extractor.SplitPdfBySections(tempOutput, validatedSections, pageCount);
    
    // Step 4: Extract data from sections
    Console.WriteLine("Step 4: Extracting data from sections...");
    await extractor.ExtractAndSaveDataFromSectionAsync(sectionList, confidenceThreshold: 0.1);

    var extractedJson = await extractor.ExtractDataFromSectionsAsync(sectionList, confidenceThreshold: 0.2);

    // Step 5: Save results
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
    Console.WriteLine("Step 5: Saving results...");
    await File.WriteAllTextAsync(
        Path.Combine("data", $"result_{timestamp}.json"), 
        extractedJson, 
        Encoding.UTF8);
    // Step 6: Optionally convert to Markdown
    Console.WriteLine("Step 6: Converting JSON to Markdown...");
    var markdown = JsonToMarkdownConverter.Convert(extractedJson);
    await File.WriteAllTextAsync(
        Path.Combine("data", $"result_{timestamp}.md"), 
        markdown, 
        Encoding.UTF8);

    Console.WriteLine("Data extraction completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

Console.WriteLine("Press Enter to exit.");
Console.ReadLine();
