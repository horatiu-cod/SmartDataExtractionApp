using System.Text;
using System.Text.Json;

namespace SmartDataExtraction;

internal class JsonToMarkdownConverter
{
    public string Convert(string jsonContent)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var root = JsonSerializer.Deserialize<Root>(jsonContent, options);

        if (root?.Pages == null) return string.Empty;

        var sb = new StringBuilder();
        string? lastContent = null; // Used for simple deduplication

        foreach (var page in root.Pages)
        {
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
                    case "Number":
                        sb.AppendLine(obj.Content?.ToString());
                        break;
                    case "Table":
                        sb.AppendLine(ConvertTableToMarkdown(obj.Rows));
                        break;
                }
                sb.AppendLine();
            }
        }

        return sb.ToString().Trim();
    }

    private string ConvertTableToMarkdown(List<TableRow> rows)
    {
        if (rows == null || rows.Count == 0) return string.Empty;

        // Transform the nested JSON row/cell structure into a 2D list of strings
        var tableData = new List<List<string>>();
        foreach (var row in rows)
        {
            var rowData = row.Cells
                .OrderBy(c => c.ColStart)
                .Select(c => c.Content?.Value ?? "")
                .ToList();
            tableData.Add(rowData);
        }

        return ToMarkdownTable(tableData);
    }

    // Reusing the logic from the previous method conversion
    private string ToMarkdownTable(List<List<string>> table)
    {
        if (table.Count == 0) return "";
        // Calculate column widths based on the longest cell in each column
        int numCols = table.Max(r => r.Count);
        int[] colWidths = Enumerable.Range(0, numCols)
            .Select(i => table.Max(row => i < row.Count ? (row[i]?.Length ?? 0) : 0))
            .Select(w => Math.Max(w, 3)) // Ensure minimum width for separators
            .ToArray();

        string FmtRow(List<string> row)
        {
            var cells = Enumerable.Range(0, numCols)
                .Select(i => (i < row.Count ? row[i] : "").PadRight(colWidths[i]));
            return "|" + string.Join("|", cells) + "|";
        }

        var sb = new StringBuilder();
        sb.AppendLine(FmtRow(table[0])); // Header
        sb.AppendLine("|" + string.Join("|", colWidths.Select(w => new string('-', w))) + "|"); // Separator

        foreach (var row in table.Skip(1))
        {
            sb.AppendLine(FmtRow(row));
        }

        return sb.ToString();
    }

    #region DTO Classes
    public class Root { public List<Page> Pages { get; set; } }
    public class Page { public List<PageObject>? PageObjects { get; set; } }
    public class PageObject
    {
        public string Type { get; set; }
        public object Content { get; set; } // Can be string or TableContent
        public List<TableRow>? Rows { get; set; }
    }
    public class TableRow { public List<TableCell> Cells { get; set; } }
    public class TableCell
    {
        public int ColStart { get; set; }
        public CellContent Content { get; set; }
    }
    public class CellContent { public string Value { get; set; } }
    #endregion

}
