using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using SharedCore.Services;
using TechnicianApp.ViewModels;

namespace TechnicianApp
{
    public partial class App : Application
    {
        public static string? PendingSessionId { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var themeService = new ThemeSettingsService();
                var savedTheme = themeService.GetSavedTheme();
                RequestedThemeVariant = savedTheme.Equals("Light", StringComparison.OrdinalIgnoreCase)
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark;

                var apiClient = new ApiClient();
                var rustdeskService = new RustDeskService();

                var mainVm = new TechnicianMainViewModel(apiClient, rustdeskService, themeService);

                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainVm
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
