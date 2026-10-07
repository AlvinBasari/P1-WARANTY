using System;
using Avalonia.Data.Converters;

namespace CustomerApp.ViewModels
{
    public static class MainConverters
    {
        public static readonly IValueConverter CategoryEquals =
            new FuncValueConverter<string, string, bool>((selected, target) =>
                string.Equals(selected, target, StringComparison.OrdinalIgnoreCase));

        public static readonly IValueConverter NotEmpty =
            new FuncValueConverter<string?, bool>(val => !string.IsNullOrWhiteSpace(val));
    }
}

