using System;
using CustomerApp.ViewModels;
using SharedCore.Services;
using Xunit;

namespace DesktopApp.Tests
{
    public class ThemeServiceTests
    {
        [Fact]
        public void ThemeSettingsService_SaveAndGetTheme_WorksCorrectly()
        {
            var service = new ThemeSettingsService();

            service.SaveTheme("Light");
            var loaded = service.GetSavedTheme();
            Assert.Equal("Light", loaded);

            service.SaveTheme("Dark");
            var loadedDark = service.GetSavedTheme();
            Assert.Equal("Dark", loadedDark);
        }

        [Fact]
        public void MainViewModel_ToggleTheme_ChangesStateAndButtonText()
        {
            var apiClient = new ApiClient();
            var deviceService = new DeviceIdentifierService();
            var rustdeskService = new RustDeskService();

            var mainVm = new MainViewModel(apiClient, deviceService, rustdeskService);

            var initialDark = mainVm.IsDarkMode;
            mainVm.ToggleThemeCommand.Execute(null);

            Assert.NotEqual(initialDark, mainVm.IsDarkMode);
            if (mainVm.IsDarkMode)
            {
                Assert.Equal("Mode Terang", mainVm.ThemeButtonText);
                Assert.Equal("IconSun", mainVm.ThemeButtonIcon);
            }
            else
            {
                Assert.Equal("Mode Gelap", mainVm.ThemeButtonText);
                Assert.Equal("IconMoon", mainVm.ThemeButtonIcon);
            }
        }
    }
}
