using System;
using System.IO;
using System.Text.Json;

namespace SharedCore.Services
{
    public class ThemeSettingsService
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "JTSVantageSupport"
        );

        private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "theme.json");

        public class ThemeConfig
        {
            public string ThemeVariant { get; set; } = "Dark";
        }

        public string GetSavedTheme()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var config = JsonSerializer.Deserialize<ThemeConfig>(json);
                    if (config != null && !string.IsNullOrWhiteSpace(config.ThemeVariant))
                    {
                        return config.ThemeVariant.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
                    }
                }
            }
            catch
            {
                // Fallback to default Dark theme if read fails
            }

            return "Dark";
        }

        public void SaveTheme(string themeVariant)
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                var config = new ThemeConfig { ThemeVariant = themeVariant };
                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                // Silently ignore write failures
            }
        }
    }
}
