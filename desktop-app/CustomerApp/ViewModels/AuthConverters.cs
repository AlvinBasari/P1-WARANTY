using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace CustomerApp.ViewModels
{
    public static class AuthConverters
    {
        public static readonly IValueConverter ModeTitle =
            new FuncValueConverter<bool, string>(isReg => isReg ? "Pendaftaran Pengguna Baru" : "Masuk ke Akun Anda");

        public static readonly IValueConverter ButtonTitle =
            new FuncValueConverter<bool, string>(isReg => isReg ? "Daftar & Hubungkan BIOS" : "Masuk");

        public static readonly IValueConverter ToggleTitle =
            new FuncValueConverter<bool, string>(isReg => isReg ? "Sudah punya akun? Masuk di sini" : "Belum punya akun? Daftar baru di sini");

        public static readonly IValueConverter StatusBrush =
            new FuncValueConverter<bool, IBrush>(online => online ? new SolidColorBrush(Color.Parse("#10B981")) : new SolidColorBrush(Color.Parse("#EF4444")));

        public static readonly IValueConverter StatusTextBrush =
            new FuncValueConverter<bool, IBrush>(online => online ? new SolidColorBrush(Color.Parse("#34D399")) : new SolidColorBrush(Color.Parse("#F87171")));

        public static readonly IValueConverter TestingTitle =
            new FuncValueConverter<bool, string>(testing => testing ? "Memeriksa..." : "⚡ Tes Koneksi");
    }
}
