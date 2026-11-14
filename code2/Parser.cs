using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using ExcelDataReader;

namespace ConsoleBasedParser;

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

        // Читаем файл через ExcelDataReader: он сам понимает .xls, .xlsx,
        // .xlsm и .xlsb — парсеру больше не нужен ClosedXML.
        var table = ReadSheet(path);

         var header = FindHeaderRow(table);
        if (header is null)
            throw new InvalidOperationException("Не найдена строка с заголовками.");

        var h = header.Value; // nullable-кортеж: работаем через Value
        var culture = CultureInfo.GetCultureInfo("ru-RU");

        for (int r = h.Row + 1; r < table.Count; r++)
        {
            var cells = table[r];
            string fullName = CellText(cells, h.Name);
            string subject = CellText(cells, h.Subject);

            if (fullName.Length == 0 && subject.Length == 0)
                continue; // пустая строка

            if (fullName.Length == 0)
            {
                problems.Add($"строка {r + 1}: нет ФИО");
                continue;
            }

            string dateText = CellText(cells, h.Date);
            DateTime? date = ParseDate(dateText, culture);
            if (date is null)
                problems.Add($"строка {r + 1}: не удалось прочитать «дату»: «{dateText}»");

            string gradeText = CellText(cells, h.Grade);
            int? grade = ParseGrade(gradeText);
            if (grade is null)
                problems.Add($"строка {r + 1}: не удалось прочитать «оценку»: «{gradeText}»");

            records.Add(new VedomostRecord(fullName, subject, date, grade));
        }

        return (records, problems);
    }

    // Чтение первого листа файла в простую таблицу: список строк,
    // каждая строка — список ячеек-строк. Внутри заменяет ClosedXML.
    public static List<List<string>> ReadSheet(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var dataSet = reader.AsDataSet();
        if (dataSet.Tables.Count == 0)
            throw new InvalidOperationException("В файле нет ни одного листа.");

        var sheet = dataSet.Tables[0];
        var rows = new List<List<string>>();

        foreach (DataRow row in sheet.Rows)
        {
            var cells = new List<string>();
            foreach (var cell in row.ItemArray)
                cells.Add(FormatCell(cell));
            rows.Add(cells);
        }

        return rows;
    }

    // Числа ExcelDataReader отдаёт как double (5 -> "5"), даты — как DateTime:
    // приводим их к строкам в едином виде, чтобы логика разбора не менялась.
    private static string FormatCell(object? cell)
    {
        if (cell is null) return "";
        if (cell is DateTime dt) return dt.ToString("dd.MM.yyyy");
        return cell.ToString()?.Trim() ?? "";
    }

    private (int Row, int Name, int Subject, int Date, int Grade)? FindHeaderRow(List<List<string>> table)
    {
        int limit = Math.Min(_config.HeaderSearchDepth, table.Count);

        for (int r = 0; r < limit; r++)
        {
            int? name = FindColumn(table[r], _config.NameKeyword);
            int? subject = FindColumn(table[r], _config.SubjectKeyword);
            int? date = FindColumn(table[r], _config.DateKeyword);
            int? grade = FindColumn(table[r], _config.GradeKeyword);

            if (name.HasValue && subject.HasValue && date.HasValue && grade.HasValue)
                return (r, name.Value, subject.Value, date.Value, grade.Value);
        }

        return null;
    }

    private static int? FindColumn(List<string> cells, string keyword)
    {
        for (int c = 0; c < cells.Count; c++)
        {
            string text = cells[c].Trim().ToLowerInvariant();
            if (text.Contains(keyword))
                return c + 1; // колонки нумеруем с единицы, как в Excel
        }
        return null;
    }

    private static string CellText(List<string> cells, int column) =>
        column >= 1 && column <= cells.Count ? cells[column - 1].Trim() : "";

    private static DateTime? ParseDate(string text, CultureInfo culture)
    {
        text = text.Trim();
        if (text.Length == 0)
            return null;

        if (DateTime.TryParseExact(text, "dd.MM.yyyy", culture, DateTimeStyles.None, out var d))
            return d;

        return DateTime.TryParse(text, culture, DateTimeStyles.None, out d) ? d : null;
    }

    private static int? ParseGrade(string text)
    {
        text = text.Trim();
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