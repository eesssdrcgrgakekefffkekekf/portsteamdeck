using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using NebulaAuth.Linux.Services;
using NebulaAuth.Linux.ViewModels;
using NebulaAuth.Linux.Views;

namespace NebulaAuth.Linux;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mafileService = new MafileService();
            var steamGuardService = new SteamGuardService();
            var settingsService = new SettingsService();
            var proxyService = new ProxyService();
            var confirmationService = new ConfirmationService(mafileService, proxyService);

            var vm = new MainWindowViewModel(
                mafileService,
                steamGuardService,
                settingsService,
                proxyService,
                confirmationService);

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
