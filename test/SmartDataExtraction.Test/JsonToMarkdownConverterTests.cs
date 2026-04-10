using System;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Syncfusion.Pdf;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SmartDataExtraction.Test;

public class JsonToMarkdownConverterTests
{

    [Fact]
    public void Table_MultilineCell_UsesLongestLineWidth()
    {
        var table = new[]
        {
            new[] { "Header1", "Header2" },
            new[] { "A", "short\nverylong" }
        };

        var json = new
        {
            Pages = new[]
            {
                new
                {
                    PageObjects = new[]
                    {
                        new
                        {
                            Type = "Table",
                            Rows = new[]
                            {
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[0][0] } }, new { ColStart = 1, Content = new { Value = table[0][1] } } } },
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[1][0] } }, new { ColStart = 1, Content = new { Value = table[1][1] } } } }
                            }
                        }
                    }
                }
            }
        };

        string jsonStr = JsonSerializer.Serialize(json);
        string result = SmartDataExtraction.JsonToMarkdownConverter.Convert(jsonStr);

        // Build expected using same visible-width rules
        var tableData = new List<List<string>> { table[0].ToList(), table[1].ToList() };
        var widths = ComputeColWidths(tableData);

        string expectedHeader = "|" + PadToWidth(table[0][0], widths[0]) + "|" + PadToWidth(table[0][1], widths[1]) + "|" + "\n";
        string expectedSep = "|" + new string('-', widths[0]) + "|" + new string('-', widths[1]) + "|" + "\n";

        string expectedFull = expectedHeader + expectedSep + "|" + PadToWidth(table[1][0], widths[0]) + "|" + PadToWidth(table[1][1], widths[1]) + "|" + "\n";
        Assert.Equal(expectedFull.Trim(), result.Replace("\r\n", "\n").Trim());
    }

    [Fact]
    public void Table_WideCharacters_EmojiAndCjk_AreCountedAsWide()
    {
        var table = new[]
        {
            new[] { "Name", "項目" },
            new[] { "🙂", "字" }
        };

        var json = new
        {
            Pages = new[]
            {
                new
                {
                    PageObjects = new[]
                    {
                        new
                        {
                            Type = "Table",
                            Rows = new[]
                            {
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[0][0] } }, new { ColStart = 1, Content = new { Value = table[0][1] } } } },
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[1][0] } }, new { ColStart = 1, Content = new { Value = table[1][1] } } } }
                            }
                        }
                    }
                }
            }
        };

        string jsonStr = JsonSerializer.Serialize(json);
        string result = SmartDataExtraction.JsonToMarkdownConverter.Convert(jsonStr);

        var tableData = new List<List<string>> { table[0].ToList(), table[1].ToList() };
        var widths = ComputeColWidths(tableData);

        string expectedHeader = "|" + PadToWidth(table[0][0], widths[0]) + "|" + PadToWidth(table[0][1], widths[1]) + "|" + "\n";
        string expectedSep = "|" + new string('-', widths[0]) + "|" + new string('-', widths[1]) + "|" + "\n";

        string expectedFull2 = expectedHeader + expectedSep + "|" + PadToWidth(table[1][0], widths[0]) + "|" + PadToWidth(table[1][1], widths[1]) + "|" + "\n";
        Assert.Equal(expectedFull2.Trim(), result.Replace("\r\n", "\n").Trim());
    }

    [Fact]
    public void Table_MultiRowAndEmptyCells_FormattingExact()
    {
        var table = new[]
        {
            new[] { "H1", "H2", "H3" },
            new[] { "r1c1", "r1c2", "r1c3" },
            new[] { "r2c1", "", "r2c3" },
            new[] { "r3c1" }
        };

        var json = new
        {
            Pages = new[]
            {
                new
                {
                    PageObjects = new[]
                    {
                        new
                        {
                            Type = "Table",
                            Rows = new[]
                            {
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[0][0] } }, new { ColStart = 1, Content = new { Value = table[0][1] } }, new { ColStart = 2, Content = new { Value = table[0][2] } } } },
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[1][0] } }, new { ColStart = 1, Content = new { Value = table[1][1] } }, new { ColStart = 2, Content = new { Value = table[1][2] } } } },
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[2][0] } }, new { ColStart = 1, Content = new { Value = table[2][1] } }, new { ColStart = 2, Content = new { Value = table[2][2] } } } },
                                new { Cells = new[] { new { ColStart = 0, Content = new { Value = table[3][0] } } } }
                            }
                        }
                    }
                }
            }
        };

        string jsonStr = JsonSerializer.Serialize(json);
        string result = SmartDataExtraction.JsonToMarkdownConverter.Convert(jsonStr).Replace("\r\n", "\n");

        var tableData = new List<List<string>> { table[0].ToList(), table[1].ToList(), table[2].ToList(), table[3].ToList() };
        var widths = ComputeColWidths(tableData);

        var sb = new StringBuilder();
        // header
        sb.Append("|");
        for (int i = 0; i < widths.Length; i++) sb.Append(PadToWidth(table[0][i], widths[i]) + "|");
        sb.Append("\n");
        // separator
        sb.Append("|");
        for (int i = 0; i < widths.Length; i++) sb.Append(new string('-', widths[i]) + "|");
        sb.Append("\n");
        // rows
        for (int r = 1; r < tableData.Count; r++)
        {
            var row = tableData[r];
            sb.Append("|");
            for (int i = 0; i < widths.Length; i++)
            {
                string cell = i < row.Count ? row[i] : "";
                sb.Append(PadToWidth(cell, widths[i]) + "|");
            }
            sb.Append("\n");
        }

        string expected = sb.ToString().Trim();

        // strict equality including internal spacing
        Assert.Equal(expected, result.Trim());
    }

    [Fact]
    public void SplitPdfByFixedNumber_FileNotFound_Throws()
    {
        var extractor = new SmartDataExtraction.TextExtractor("", "");
        Assert.Throws<FileNotFoundException>(() => extractor.SplitPdfByFixedNumber("nonexistent_input.pdf"));
    }


    // Helpers used by tests to compute visible widths and padding (same rules as production code)
    private static int[] ComputeColWidths(List<List<string>> table)
    {
        int numCols = table.Max(r => r.Count);
        int[] colWidths = new int[numCols];
        foreach (var row in table)
        {
            for (int i = 0; i < numCols; i++)
            {
                string cell = i < row.Count ? row[i] ?? "" : "";
                var lines = cell.Replace("\r\n", "\n").Split('\n');
                int maxLineWidth = 0;
                foreach (var line in lines)
                {
                    int w = GetDisplayWidth(line);
                    if (w > maxLineWidth) maxLineWidth = w;
                }
                if (maxLineWidth > colWidths[i]) colWidths[i] = maxLineWidth;
            }
        }
        for (int i = 0; i < colWidths.Length; i++) colWidths[i] = Math.Max(colWidths[i], 3);
        return colWidths;
    }

    private static string PadToWidth(string s, int targetWidth)
    {
        int cur = GetDisplayWidth(s);
        if (cur >= targetWidth) return s;
        return s + new string(' ', targetWidth - cur);
    }

    private static int GetDisplayWidth(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        var indices = StringInfo.ParseCombiningCharacters(s);
        int width = 0;
        for (int i = 0; i < indices.Length; i++)
        {
            int start = indices[i];
            int len = ((i + 1) < indices.Length ? indices[i + 1] : s.Length) - start;
            string grapheme = s.Substring(start, len);
            var firstRune = grapheme.EnumerateRunes().FirstOrDefault();
            if (firstRune.Value == 0) continue;
            if (Rune.IsControl(firstRune)) continue;
            width += IsWide(firstRune) ? 2 : 1;
        }
        return width;
    }

    private static bool IsWide(Rune r)
    {
        int cp = r.Value;
        return
            (cp >= 0x1100 && cp <= 0x115F) ||
            (cp >= 0x2E80 && cp <= 0xA4CF) ||
            (cp >= 0xAC00 && cp <= 0xD7A3) ||
            (cp >= 0xF900 && cp <= 0xFAFF) ||
            (cp >= 0xFE10 && cp <= 0xFE19) ||
            (cp >= 0xFE30 && cp <= 0xFE6F) ||
            (cp >= 0xFF00 && cp <= 0xFF60) ||
            (cp >= 0x1F300 && cp <= 0x1F64F) ||
            (cp >= 0x1F680 && cp <= 0x1F6FF) ||
            (cp >= 0x1F900 && cp <= 0x1F9FF) ||
            (cp >= 0x1FA70 && cp <= 0x1FAFF);
    }
}
