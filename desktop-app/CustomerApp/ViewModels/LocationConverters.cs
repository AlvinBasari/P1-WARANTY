using System;
using Avalonia.Data.Converters;

namespace CustomerApp.ViewModels
{
    public static class LocationConverters
    {
        public static readonly IValueConverter SaveButtonTitle =
            new FuncValueConverter<bool, string>(loading => loading ? "Menyimpan Titik Lokasi..." : "Simpan Titik Lokasi Perangkat");
    }
}
