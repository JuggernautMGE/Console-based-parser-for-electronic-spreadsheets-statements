
using System.Text.Json;
using ClosedXML.Excel;

var path = args.Length > 0 ? args[0] : "vedomost.xlsx";
if (!File.Exists(path))
{
    Console.Error.WriteLine($"Файл не найден: {path}");
    return 1;
}

var config = new VedomostConfig();
var parser = new VedomostParser(config);
var rows = parser.Parse(path);

Console.WriteLine($"Распознано строк: {rows.Count}");
foreach (var r in rows)
    Console.WriteLine($"{r.TabNumber,-10} {r.FullName,-40} {r.ToPay,12:N2}");

if (parser.Errors.Count > 0)
{
    Console.WriteLine($"\nПроблемные строки: {parser.Errors.Count}");
    foreach (var e in parser.Errors)
        Console.WriteLine($"  строка {e.ExcelRow}: {e.Message}");
}

var totalRows = rows.Sum(r => r.ToPay);
Console.WriteLine($"\nСумма «к выплате» по строкам: {totalRows:N2}");

string? totalFromFile = ReadTotalCell(path);
if (decimal.TryParse(totalFromFile?.Replace(" ", "").Replace("\u00A0", "").Replace(",", "."),
                     out var fileTotal))
{
    if (Math.Abs(totalRows - fileTotal) > 0.01m)
        Console.WriteLine($"ВНИМАНИЕ: расхождение с итогом файла {fileTotal:N2} — проверьте ведомость!");
    else
        Console.WriteLine("Контрольная сумма сходится");
}

var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText("result.json", json);
Console.WriteLine("Результат сохранён в result.json");
return 0;

static string? ReadTotalCell(string path)
{
    using var wb = new XLWorkbook(path);
    var ws = wb.Worksheets.First();
    foreach (var row in ws.RowsUsed())
    {
        if (row.Cell(1).GetString().ToLower().Contains("итог"))
        {
            var cells = row.CellsUsed().Where(c => decimal.TryParse(
                c.GetString().Replace(" ", "").Replace("\u00A0", "").Replace(",", "."), out _));
            var last = cells.LastOrDefault();
            return last?.GetString();
        }
    }
    return null;
}
