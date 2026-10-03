using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CustomerApp.ViewModels;
using CustomerApp.Views;
using SharedCore.Services;

namespace CustomerApp
{
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
                var apiClient = new ApiClient();
                var deviceService = new DeviceIdentifierService();
                var rustdeskService = new RustDeskService();

                var mainVm = new MainViewModel(apiClient, deviceService, rustdeskService);

                desktop.MainWindow = new MainWindow
                {
                    DataContext = mainVm
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
