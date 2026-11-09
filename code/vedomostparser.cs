using ClosedXML.Excel;

public class VedomostParser
{
    private readonly VedomostConfig _config;

    public List<ParseError> Errors { get; } = new();

    public VedomostParser(VedomostConfig config) => _config = config;

    public List<VedomostRow> Parse(string path, string? sheetName = null)
    {
        var rows = new List<VedomostRow>();
        Errors.Clear();

        using var workbook = new XLWorkbook(path);
        var ws = sheetName != null ? workbook.Worksheet(sheetName) : workbook.Worksheets.First();

        int headerRow = FindHeaderRow(ws);
        if (headerRow == -1)
            throw new InvalidOperationException("Не найдена строка с заголовками.");

        var headerMap = BuildHeaderMap(ws, headerRow);
          var lastUsedRow = ws.LastRowUsed();
        int lastRow = 0;
        
if (lastUsedRow != null) lastRow = lastUsedRow.RowNumber();

        for (int r = headerRow + 1; r <= lastRow; r++)
        {
            var row = ws.Row(r);
            if (row.IsEmpty()) continue;

            string tab = GetCell(row, headerMap, _config.TabKeyword);

            // пропускаем итоговые строки
            string firstCell = row.Cell(1).GetString().ToLower();
            if (_config.SkipKeywords.Any(k => firstCell.Contains(k))) continue;
            if (string.IsNullOrWhiteSpace(tab))
            {
                Errors.Add(new ParseError { ExcelRow = r, Message = "Пустой табельный номер" });
                continue;
            }

            rows.Add(new VedomostRow
            {
                TabNumber = tab.Trim(),
                FullName  = GetCell(row, headerMap, _config.NameKeyword),
                Period    = GetCell(row, headerMap, _config.PeriodKeyword),
                Accrued   = ParseDecimal(GetCell(row, headerMap, _config.AccruedKeyword), r, "начислено"),
                Withheld  = ParseDecimal(GetCell(row, headerMap, _config.WithheldKeyword), r, "удержано"),
                ToPay     = ParseDecimal(GetCell(row, headerMap, _config.ToPayKeyword), r, "к выплате")
            });
        }
        return rows;
    }

     int FindHeaderRow(IXLWorksheet ws)
    {
        int limit = _config.HeaderSearchDepth;
        var lastUsed = ws.LastRowUsed();
        if (lastUsed != null && lastUsed.RowNumber() < limit) limit = lastUsed.RowNumber();

        for (int r = 1; r <= limit; r++)
        {
            var cells = ws.Row(r).CellsUsed()
                          .Select(c => c.GetString().ToLower())
                          .ToList();
            if (cells.Any(c => c.Contains(_config.TabKeyword)) &&
                cells.Any(c => c.Contains(_config.NameKeyword)))
                return r;
        }
        return -1;
    }

    Dictionary<int, string> BuildHeaderMap(IXLWorksheet ws, int headerRow)
    {
        var map = new Dictionary<int, string>();
        foreach (var cell in ws.Row(headerRow).CellsUsed())
            map[cell.Address.ColumnNumber] = cell.GetString().Trim().ToLower();
        return map;
    }

    string GetCell(IXLRow row, Dictionary<int, string> map, string keyword)
    {
        var col = map.FirstOrDefault(kv => kv.Value.Contains(keyword)).Key;
        return col == 0 ? "" : row.Cell(col).GetString().Trim();
    }

    decimal ParseDecimal(string s, int excelRow, string fieldName)
    {
        var clean = s.Replace(" ", "").Replace("\u00A0", "").Replace(",", ".");
        if (decimal.TryParse(clean, out var d)) return d;

        Errors.Add(new ParseError
        {
            ExcelRow = excelRow,
            Message = $"Не удалось прочитать «{fieldName}»: «{s}»"
        });
        return 0m;
    }
}