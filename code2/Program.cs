using Avalonia;

namespace ConsoleBasedParser;

class Program
{
    // Точка входа программы.
    // [STAThread] обязателен для GUI-приложений Windows: интерфейс работает
    // в однопоточном режиме COM (STA), без него окно не запустится.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Конфигурация приложения: какой класс считать приложением (App),
    // определение платформы (Windows/Linux/macOS), шрифт и логирование
    // в консоль отладочных сообщений.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}