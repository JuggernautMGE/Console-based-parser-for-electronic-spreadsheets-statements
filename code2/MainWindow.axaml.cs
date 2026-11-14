using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace ConsoleBasedParser;

// Логика главного окна (code-behind для MainWindow.axaml).
// Класс partial: одну половину пишем мы, вторую (InitializeComponent и поля
// элементов управления) генерирует Avalonia из XAML-разметки.
public partial class MainWindow : Window
{
    // Текущее состояние окна: разобранные записи, проблемные строки
    // и путь к исходному файлу. Заполняются при открытии ведомости.
    private List<VedomostRecord> _records = new();
    private List<string> _problems = new();
    private string? _sourcePath;

    public MainWindow() => InitializeComponent();

    // Кнопка «Открыть ведомость…».
    // Открывает системный диалог выбора файла, передаёт файл парсеру
    // и выводит результат в окно.
    private async void OnOpenFile(object? sender, RoutedEventArgs e)
    {
        // Диалог выбора файла: один файл, форматы Excel (старые и новые).
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Выберите файл ведомости",
            AllowMultiple = false,
             FileTypeFilter = new FilePickerFileType[]
            {
                new("Файлы Excel") { Patterns = new[] { "*.xls", "*.xlsx", "*.xlsm", "*.xlsb", "*.xltx", "*.xltm" } }
            }
        });

        // Пользователь закрыл диалог, не выбрав файл — тихо выходим.
        if (files.Count == 0) return;
        _sourcePath = files[0].Path.LocalPath;

        // Парсер не знает ничего об интерфейсе: принимает путь, возвращает данные.
        var parser = new VedomostParser(new VedomostConfig());
        try
        {
            (_records, _problems) = parser.Parse(_sourcePath);
        }
        catch (Exception ex)
        {
            // Файл не читается (повреждён, не Excel, нет листов) — сообщаем
            // и прячем кнопки экспорта: экспортировать нечего.
            StatusText.Text = "Ошибка: " + ex.Message;
            CsvButton.IsEnabled = JsonButton.IsEnabled = false;
            CsvButton.IsVisible = JsonButton.IsVisible = false;
            return;
        }

        // Формируем текстовый превью: путь, число строк и сама таблица
        // с выравниванием колонок по ширине (-35, -25).
        var sb = new StringBuilder();
        sb.AppendLine($"Файл: {_sourcePath}");
        sb.AppendLine($"Распознано строк: {_records.Count}");
        sb.AppendLine();
        foreach (var r in _records)
            sb.AppendLine($"{r.FullName,-35} {r.Subject,-25} {r.Date:dd.MM.yyyy}  оценка: {r.Grade}");

        // Проблемные строки показываем после основной таблицы,
        // чтобы пользователь видел, что именно не разобралось и в какой строке.
        if (_problems.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"Проблемные строки: {_problems.Count}");
            foreach (var p in _problems) sb.AppendLine("  " + p);
        }

        PreviewText.Text = sb.ToString();
        StatusText.Text = _problems.Count == 0
          ? "Разбор успешен. Выберите формат экспорта кнопкой ниже."
          : $"Разбор завершён, проблемных строк: {_problems.Count}. Выберите формат экспорта.";

        // Экспорт разрешаем только после успешного разбора файла.
        CsvButton.IsVisible = JsonButton.IsVisible = true;
        CsvButton.IsEnabled = JsonButton.IsEnabled = true;
    }

    private void OnExportCsv(object? sender, RoutedEventArgs e) => ExportAsync("csv");
    private void OnExportJson(object? sender, RoutedEventArgs e) => ExportAsync("json");

    // Общий обработчик для обеих кнопок экспорта: различие только в формате.
    // Открывает диалог сохранения и вызывает нужный экспортёр.
    private async void ExportAsync(string format)
    {
        if (_sourcePath is null) return;
        string ext = format == "csv" ? "csv" : "json";

        // Имя файла по умолчанию берём из исходного: vedomost.xlsx -> vedomost.csv
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Куда сохранить результат",
            SuggestedFileName = Path.ChangeExtension(Path.GetFileName(_sourcePath), ext),
            DefaultExtension = ext
        });
        if (file is null) return;

        string path = file.Path.LocalPath;

        try
        {
            if (format == "csv") CsvExporter.Save(_records, path);
            else JsonExporter.Save(_records, path);

            StatusText.Text = $"Готово: {path}";
        }
        catch (Exception ex)
        {
            // Например, файл открыт в Excel и заблокирован — не падаем,
            // а показываем сообщение в строке статуса.
            StatusText.Text = "Не удалось сохранить: " + ex.Message;
        }
    }
}