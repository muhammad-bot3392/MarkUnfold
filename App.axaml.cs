using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using MarkItDownGUI.Services;
using MarkItDownGUI.ViewModels;
using MarkItDownGUI.Views;

namespace MarkItDownGUI;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Apply saved theme before showing window
        var savedTheme = Preferences.GetTheme();
        RequestedThemeVariant = savedTheme == "light"
            ? ThemeVariant.Light
            : ThemeVariant.Dark;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
