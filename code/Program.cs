using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;

var config = new VedomostConfig();

if (args.Length == 0)
{
    Console.WriteLine("Использование: dotnet run -- <файл_ведомости.xlsx>");
    return 1;
}

string inputPath = args[0];

if (!File.Exists(inputPath))
{
    Console.WriteLine($"Ошибка: файл «{inputPath}» не найден.");
    return 1;
}

// ---- разбор файла ----
var parser = new VedomostParser(config);
var (records, problems) = parser.Parse(inputPath);

Console.WriteLine($"Распознано строк: {records.Count}");
foreach (var rec in records)
    Console.WriteLine($"{rec.FullName,-35} {rec.Subject,-25} {rec.Date:dd.MM.yyyy}  оценка: {rec.Grade}");

if (problems.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine($"Проблемные строки: {problems.Count}");
    foreach (var problem in problems)
        Console.WriteLine("  " + problem);
}

// ---- выбор формата вывода ----
string format;
while (true)
{
    Console.WriteLine();
    Console.WriteLine("Выберите формат сохранения результата:");
    Console.WriteLine("  1 — CSV");
    Console.WriteLine("  2 — JSON");
    Console.Write("Введите 1 или 2: ");

    string? answer = Console.ReadLine()?.Trim();
    if (answer == "1") { format = "csv"; break; }
    if (answer == "2") { format = "json"; break; }

    Console.WriteLine("Не понял ответ — введите 1 или 2.");
}

string outputPath = Path.ChangeExtension(inputPath, format);

if (format == "json")
    JsonExporter.Save(records, outputPath);
else
    CsvExporter.Save(records, outputPath);

Console.WriteLine();
Console.WriteLine($"Результат сохранён в {outputPath}");
return 0;

public class VedomostConfig
{
    public string NameKeyword { get; init; } = "фио";
    public string SubjectKeyword { get; init; } = "дисциплин";
    public string DateKeyword { get; init; } = "дат";
    public string GradeKeyword { get; init; } = "оцен";
    public int HeaderSearchDepth { get; init; } = 10;
}

public record VedomostRecord(string FullName, string Subject, DateTime? Date, int? Grade);

public class VedomostParser
{
    private readonly VedomostConfig _config;

    public VedomostParser(VedomostConfig config) => _config = config;

    public (List<VedomostRecord> Records, List<string> Problems) Parse(string path)
    {
        var records = new List<VedomostRecord>();
        var problems = new List<string>();

        using var workbook = new XLWorkbook(path);
        var sheet = workbook.Worksheets.First();

        var header = FindHeaderRow(sheet);
        if (header is null)
            throw new InvalidOperationException("Не найдена строка с заголовками.");

        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        for (int r = header.Value.Row + 1; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            string fullName = CellText(row, header.Value.Name);
            string subject = CellText(row, header.Value.Subject);

            if (fullName.Length == 0 && subject.Length == 0)
                continue; // пустая строка

            if (fullName.Length == 0)
            {
                problems.Add($"строка {r}: нет ФИО");
                continue;
            }

            DateTime? date = ParseDate(row.Cell(header.Value.Date), culture);
            if (date is null)
                problems.Add($"строка {r}: не удалось прочитать «дату»: «{row.Cell(header.Value.Date).GetString()}»");

            int? grade = ParseGrade(row.Cell(header.Value.Grade));
            if (grade is null)
                problems.Add($"строка {r}: не удалось прочитать «оценку»: «{row.Cell(header.Value.Grade).GetString()}»");

            records.Add(new VedomostRecord(fullName, subject, date, grade));
        }

        return (records, problems);
    }

    private (int Row, int Name, int Subject, int Date, int Grade)? FindHeaderRow(IXLWorksheet sheet)
    {
        int lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        int limit = Math.Min(_config.HeaderSearchDepth, lastRow);

        for (int r = 1; r <= limit; r++)
        {
            int? name = FindColumn(sheet.Row(r), _config.NameKeyword);
            int? subject = FindColumn(sheet.Row(r), _config.SubjectKeyword);
            int? date = FindColumn(sheet.Row(r), _config.DateKeyword);
            int? grade = FindColumn(sheet.Row(r), _config.GradeKeyword);

            if (name.HasValue && subject.HasValue && date.HasValue && grade.HasValue)
                return (r, name.Value, subject.Value, date.Value, grade.Value);
        }

        return null;
    }

    private static int? FindColumn(IXLRow row, string keyword)
    {
        foreach (var cell in row.CellsUsed())
        {
            string text = cell.GetString().Trim().ToLowerInvariant();
            if (text.Contains(keyword))
                return cell.Address.ColumnNumber;
        }
        return null;
    }

    private static string CellText(IXLRow row, int column) =>
        row.Cell(column).GetString().Trim();

    private static DateTime? ParseDate(IXLCell cell, CultureInfo culture)
    {
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime();

        string text = cell.GetString().Trim();
        if (text.Length == 0)
            return null;

        if (DateTime.TryParseExact(text, "dd.MM.yyyy", culture, DateTimeStyles.None, out var d))
            return d;

        return DateTime.TryParse(text, culture, DateTimeStyles.None, out d) ? d : null;
    }

    private static int? ParseGrade(IXLCell cell)
    {
        string text = cell.GetString().Trim();
        string head = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return int.TryParse(head, out int grade) ? grade : null;
    }
}

public static class CsvExporter
{
    public static void Save(IEnumerable<VedomostRecord> records, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ФИО;Дисциплина;Дата;Оценка");

        foreach (var r in records)
        {
            sb.AppendLine(string.Join(';',
                Escape(r.FullName),
                Escape(r.Subject),
                r.Date?.ToString("dd.MM.yyyy") ?? "",
                r.Grade?.ToString() ?? ""));
        }

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }

    private static string Escape(string? value)
    {
        value ??= "";
        if (value.Contains(';') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

public static class JsonExporter
{
    public static void Save(IEnumerable<VedomostRecord> records, string path)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var list = records.Select(r => new
        {
            fullName = r.FullName,
            subject = r.Subject,
            date = r.Date?.ToString("dd.MM.yyyy"),
            grade = r.Grade
        });

        File.WriteAllText(path, JsonSerializer.Serialize(list, options), new UTF8Encoding(false));
    }
}