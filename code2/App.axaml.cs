using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ConsoleBasedParser;

// Корневой объект приложения. Отвечает на два вопроса:
// какие ресурсы и темы использовать (заданы в App.axaml) и что показать при старте.
public class App : Application
{
    // Загружает XAML-описание этого класса (App.axaml) — там подключена тема FluentTheme.
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    // Вызывается один раз при старте, когда среда сообщила тип приложения.
    // Для десктопа создаём главное окно — оно станет корневым окном программы.
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow();
        base.OnFrameworkInitializationCompleted();
    }
}