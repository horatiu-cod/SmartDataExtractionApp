using System.Collections.Generic;
using SmartDataExtraction;

namespace SmartDataExtraction.Test;

public class TextExtractorTests
{
    private class FakePdfService : IPdfService
    {
        private readonly Dictionary<int, string[]> _mapping;
        public FakePdfService(Dictionary<int, string[]> mapping)
        {
            _mapping = mapping;
        }

        public Dictionary<int, string[]> FindText(string pdfPath, List<string> pageHeaders)
        {
            // Return a shallow copy to mimic behavior
            return new Dictionary<int, string[]>(_mapping);
        }
    }

    [Fact]
    public void FindAndValidateSections_ValidMatches_ReturnsValidated()
    {
        var pageHeaders = new List<string> { "H1", "H2" };
        // Return H1 at page 1, H2 at page 3 (order matches expected header indices)
        var mapping = new Dictionary<int, string[]>
        {
            { 1, new[] { "H1" } },
            { 3, new[] { "H2" } }
        };

        var fake = new FakePdfService(mapping);
        var extractor = new TextExtractor("", fake, null, "tmp_results");

        var validated = extractor.FindAndValidateSections("ignored.pdf", pageHeaders);

        Assert.True(validated.ContainsKey(0));
        Assert.Equal("Informatii_Generale", validated[0]);
        Assert.Equal("H1", validated[1]);
        Assert.Equal("H2", validated[3]);
    }

    [Fact]
    public void FindAndValidateSections_Mismatched_FirstRemoved()
    {
        var pageHeaders = new List<string> { "H1", "H2" };
        // First found is H2 (mismatched index 0), second is H1
        var mapping = new Dictionary<int, string[]>
        {
            { 2, new[] { "H2" } },
            { 4, new[] { "H1" } }
        };

        var fake = new FakePdfService(mapping);
        var extractor = new TextExtractor("", fake, null, "tmp_results");

        var validated = extractor.FindAndValidateSections("ignored.pdf", pageHeaders);

        Assert.True(validated.ContainsKey(0));
        Assert.Equal("Informatii_Generale", validated[0]);
        // The mismatched first entry should be removed and the remaining H1 (which becomes index 0 in iteration) should be accepted
        Assert.Equal(2, validated.Count); // 0 + one validated
        Assert.Contains(4, validated.Keys);
        Assert.Equal("H1", validated[4]);
    }
}
