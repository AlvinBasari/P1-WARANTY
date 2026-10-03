using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CustomerApp.ViewModels
{
    public static class ClaimConverters
    {
        private static bool IsLightTheme()
        {
            if (Application.Current == null) return false;
            return Application.Current.RequestedThemeVariant == Avalonia.Styling.ThemeVariant.Light
                || Application.Current.ActualThemeVariant == Avalonia.Styling.ThemeVariant.Light;
        }

        private static IBrush GetResourceBrush(string key, string darkFallback, string lightFallback)
        {
            bool isLight = IsLightTheme();
            var variant = isLight ? Avalonia.Styling.ThemeVariant.Light : Avalonia.Styling.ThemeVariant.Dark;

            if (Application.Current != null && Application.Current.TryGetResource(key, variant, out var res) && res is IBrush brush)
            {
                return brush;
            }
            return new SolidColorBrush(Color.Parse(isLight ? lightFallback : darkFallback));
        }

        public static readonly IValueConverter SelectedCardBg =
            new FuncValueConverter<bool, IBrush>(selected =>
                selected
                    ? GetResourceBrush("VantageAccentSubtleBrush", "#172554", "#DBEAFE")
                    : GetResourceBrush("VantageCardBrush", "#171B26", "#FFFFFF"));

        public static readonly IValueConverter SelectedCardBorder =
            new FuncValueConverter<bool, IBrush>(selected =>
                selected
                    ? GetResourceBrush("VantageAccentBrush", "#2563EB", "#2563EB")
                    : GetResourceBrush("VantageBorderBrush", "#262D3D", "#CBD5E1"));

        public static readonly IValueConverter NoticeTitle =
            new FuncValueConverter<bool, string>(isPhysical => isPhysical
                ? "📍 Alur Servis On-Site (Bypass Remote)"
                : "⚡ Alur Remote Diagnosis (RustDesk)");

        public static readonly IValueConverter NoticeDesc =
            new FuncValueConverter<bool, string>(isPhysical => isPhysical
                ? "Sistem akan melewati tahap remote dan otomatis melampirkan titik lokasi Anda yang tersimpan untuk jadwal kedatangan teknisi lapangan."
                : "Sistem akan otomatis menghasilkan Session ID RustDesk agar teknisi dapat tersambung dan mendiagnosis kendala software secara langsung.");

        public static readonly IValueConverter ButtonStateTitle =
            new FuncValueConverter<bool, string>(loading => loading ? "Mengirim Permintaan..." : "Kirim Permintaan Bantuan");

        public static readonly IValueConverter SimulationButtonTitle =
            new FuncValueConverter<bool, string>(simulating => simulating ? "Menghubungi Sistem Teknisi..." : "⚡ [Demo] Simulasikan Konfirmasi Jadwal oleh Teknisi");

        public static readonly IValueConverter ConnectButtonBg =
            new FuncValueConverter<bool, IBrush>(canConnect =>
                canConnect
                    ? GetResourceBrush("VantageAccentBrush", "#2563EB", "#2563EB")
                    : GetResourceBrush("VantageBorderBrush", "#262D3D", "#CBD5E1"));
    }
}
