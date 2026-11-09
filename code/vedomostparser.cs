using ClosedXML.Excel;

public class VedomostParser
{
private readonly VedomostConfig _config;

public VedomostParser(VedomostConfig config) => _config = config;

public List<VedomostRow> Parse(string path, string sheetName = null)
{
var rows = new List<VedomostRow>();

using var workbook = new XLWorkbook(path);
var ws = sheetName != null ? workbook.Worksheet(sheetName) : workbook.Worksheets.First();

int headerRow = FindHeaderRow(ws);
if (headerRow == -1)
throw new InvalidOperationException("Не найдена строка с заголовками.");

var headerMap = BuildHeaderMap(ws, headerRow);
var lastRow = ws.LastRowUsed().RowNumber;

for (int r = headerRow + 1; r <= lastRow; r++)
{
var row = ws.Row(r);
if (row.IsEmpty()) continue;

string tab = GetCell(row, headerMap, _config.TabKeyword);

// пропускаем итоговые строки
string firstCell = row.Cell(1).GetString().ToLower();
if (_config.SkipKeywords.Any(k => firstCell.Contains(k))) continue;
if (string.IsNullOrWhiteSpace(tab)) continue;

rows.Add(new VedomostRow
{
TabNumber = tab.Trim(),
FullName = GetCell(row, headerMap, _config.NameKeyword),
Period = GetCell(row, headerMap, _config.PeriodKeyword),
Accrued = ParseDecimal(GetCell(row, headerMap, _config.AccruedKeyword)),
Withheld = ParseDecimal(GetCell(row, headerMap, _config.WithheldKeyword)),
ToPay = ParseDecimal(GetCell(row, headerMap, _config.ToPayKeyword))
});
}
return rows;
}

// методы FindHeaderRow, BuildHeaderMap, GetCell, ParseDecimal — те же,
// только в FindHeaderRow замените 10 на _config.HeaderSearchDepth
}