using System.Globalization;
using System.Text;
using System.Text.Json;

namespace SmartDataExtraction;

public static class JsonToMarkdownConverter
{
    public static string Convert(string jsonContent)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<Root>(jsonContent, options);

        if (root?.Pages == null) return string.Empty;

        var sb = new StringBuilder();
        string? lastContent = null; // Used for simple deduplication

        foreach (var page in root.Pages)
        {
            if (page.PageObjects == null) continue;
            foreach (var obj in page.PageObjects)
            {
                // Simple deduplication: skip if same content as previous (common in OCR JSONs)
                if (obj.Content != null && obj.Content.ToString() == lastContent) continue;
                lastContent = obj.Content?.ToString();

                switch (obj.Type)
                {
                    case "Header":
                        sb.AppendLine($"# {obj.Content}");
                        break;
                    case "Paragraph title":
                        sb.AppendLine($"### {obj.Content}");
                        break;
                    case "Figure title":
                        sb.AppendLine($"#### *{obj.Content}*");
                        break;
                    case "Text":
                    case "Algorithm":
                    case "Number":
                        sb.AppendLine(obj.Content?.ToString());
                        break;
                    case "Table":
                        if (obj.Rows != null) sb.AppendLine(ConvertTableToMarkdown(obj.Rows));
                        break;
                }
                sb.AppendLine();
            }
        }

        return sb.ToString().Trim();
    }

    private static string ConvertTableToMarkdown(List<TableRow> rows)
    {
        if (rows == null || rows.Count == 0) return string.Empty;

        // Transform the nested JSON row/cell structure into a 2D list of strings
        var tableData = new List<List<string>>();
        foreach (var row in rows)
        {
            if (row.Cells == null) continue;
            var rowData = row.Cells
                .OrderBy(c => c.ColStart)
                .Select(c => c.Content?.Value ?? "")
                .ToList();
            tableData.Add(rowData);
        }

        return ToMarkdownTable(tableData);
    }

    private static string ToMarkdownTable(List<List<string>> table)
    {
        if (table.Count == 0) return "";

        int numCols = table.Max(r => r.Count);
        int[] colWidths = new int[numCols];
        foreach (var row in table)
        {
            if (row == null) continue;
            MeasureVisibleColumnWidths(row, numCols, colWidths);
        }
        for (int i = 0; i < colWidths.Length; i++) colWidths[i] = Math.Max(colWidths[i], 3);

        var sb = new StringBuilder();
        sb.AppendLine(FmtRow(table[0], numCols, colWidths)); // Header
        sb.AppendLine("|" + string.Join("|", colWidths.Select(w => new string('-', w))) + "|"); // Separator
        foreach (var row in table.Skip(1)) sb.AppendLine(FmtRow(row, numCols, colWidths));
        return sb.ToString();
    }

    private static void MeasureVisibleColumnWidths(List<string> row, int numCols, int[] colWidths)
    {
        // single-pass: compute visible width per column, account for multiline cells
        for (int i = 0; i < numCols; i++)
        {
            string cell = i < row.Count ? row[i] ?? "" : "";
            // handle multiline: take max visible width of any line
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

    private static string FmtRow(List<string> row, int numCols, int[] colWidths)
    {
        var cells = Enumerable.Range(0, numCols)
            .Select(i => PadToWidth(i < row.Count ? row[i] ?? "" : "", colWidths[i]));
        return "|" + string.Join("|", cells) + "|";
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
        // Common wide ranges: CJK, Hangul, Hiragana, Katakana, Fullwidth, Compatibility, emoji ranges
        return
            (cp >= 0x1100 && cp <= 0x115F) || // Hangul Jamo init
            (cp >= 0x2E80 && cp <= 0xA4CF) || // CJK Radicals .. Yi
            (cp >= 0xAC00 && cp <= 0xD7A3) || // Hangul Syllables
            (cp >= 0xF900 && cp <= 0xFAFF) || // CJK Compatibility Ideographs
            (cp >= 0xFE10 && cp <= 0xFE19) || // Vertical forms
            (cp >= 0xFE30 && cp <= 0xFE6F) || // CJK Compatibility Forms
            (cp >= 0xFF00 && cp <= 0xFF60) || // Fullwidth Forms
            (cp >= 0x1F300 && cp <= 0x1F64F) || // Misc symbols & pictographs
            (cp >= 0x1F680 && cp <= 0x1F6FF) || // Transport & map
            (cp >= 0x1F900 && cp <= 0x1F9FF) || // Supplemental Symbols and Pictographs
            (cp >= 0x1FA70 && cp <= 0x1FAFF);   // Symbols & Pictographs Extended-A
    }

    private static string PadToWidth(string s, int targetWidth)
    {
        int cur = GetDisplayWidth(s);
        if (cur >= targetWidth) return s;
        return s + new string(' ', targetWidth - cur);
    }
    // Reusing the logic from the previous method conversion
    //private string ToMarkdownTable(List<List<string>> table)
    //{
    //    if (table.Count == 0) return "";
    //    // Calculate column widths based on the longest cell in each column
    //    int numCols = table.Max(r => r.Count);
    //    int[] colWidths = Enumerable.Range(0, numCols)
    //        .Select(i => table.Max(row => i < row.Count ? (row[i]?.Length ?? 0) : 0))
    //        .Select(w => Math.Max(w, 3)) // Ensure minimum width for separators
    //        .ToArray();

    //    string FmtRow(List<string> row)
    //    {
    //        var cells = Enumerable.Range(0, numCols)
    //            .Select(i => (i < row.Count ? row[i] : "").PadRight(colWidths[i]));
    //        return "|" + string.Join("|", cells) + "|";
    //    }

    //    var sb = new StringBuilder();
    //    sb.AppendLine(FmtRow(table[0])); // Header
    //    sb.AppendLine("|" + string.Join("|", colWidths.Select(w => new string('-', w))) + "|"); // Separator

    //    foreach (var row in table.Skip(1))
    //    {
    //        sb.AppendLine(FmtRow(row));
    //    }

    //    return sb.ToString();
    //}

    #region DTO Classes
    public class Root { public List<Page>? Pages { get; set; } }
    public class Page { public List<PageObject>? PageObjects { get; set; } }
    public class PageObject
    {
        public string? Type { get; set; }
        public object? Content { get; set; } // Can be string or TableContent
        public List<TableRow>? Rows { get; set; }
    }
    public class TableRow { public List<TableCell>? Cells { get; set; } }
    public class TableCell
    {
        public int ColStart { get; set; }
        public CellContent? Content { get; set; }
    }
    public class CellContent { public string? Value { get; set; } }
    #endregion
}
