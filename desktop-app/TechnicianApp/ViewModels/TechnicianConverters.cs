using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace TechnicianApp.ViewModels
{
    public class DamageCategoryTextConverter : IValueConverter
    {
        public static readonly DamageCategoryTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string cat)
            {
                return cat.ToLowerInvariant() switch
                {
                    "physical" => "Servis On-Site (Fisik)",
                    "non_physical" => "Bantuan Remote (Non-Fisik)",
                    _ => cat
                };
            }
            return "Perbaikan";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class DamageCategoryBadgeBgConverter : IValueConverter
    {
        public static readonly DamageCategoryBadgeBgConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string cat && cat.Equals("physical", StringComparison.OrdinalIgnoreCase))
            {
                return SolidColorBrush.Parse("#1E293B"); // Dark slate
            }
            return SolidColorBrush.Parse("#172554"); // Blue subtle
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class DamageCategoryBadgeFgConverter : IValueConverter
    {
        public static readonly DamageCategoryBadgeFgConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string cat && cat.Equals("physical", StringComparison.OrdinalIgnoreCase))
            {
                return SolidColorBrush.Parse("#F59E0B"); // Amber
            }
            return SolidColorBrush.Parse("#60A5FA"); // Light Blue
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusTextConverter : IValueConverter
    {
        public static readonly StatusTextConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLowerInvariant() switch
                {
                    "pending" => "Menunggu Tindakan",
                    "scheduled" => "Telah Dijadwalkan",
                    "in_progress" => "Sedang Dikerjakan",
                    "completed" => "Selesai",
                    "rejected" => "Ditolak",
                    "cancelled" => "Dibatalkan",
                    "waiting_acceptance" => "Menunggu Persetujuan",
                    "connected" => "Terhubung",
                    "dijemput" => "Unit Dijemput / Diterima",
                    "di_service_center" => "Di Service Center",
                    "sedang_diperbaiki" => "Sedang Diperbaiki",
                    "penggantian_part" => "Penggantian Suku Cadang",
                    "pengujian" => "Pengujian & QC",
                    "dikembalikan" => "Selesai & Dikembalikan",
                    _ => status
                };
            }
            return "-";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusBadgeBgConverter : IValueConverter
    {
        public static readonly StatusBadgeBgConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLowerInvariant() switch
                {
                    "pending" => SolidColorBrush.Parse("#451A03"), // Amber subtle
                    "scheduled" => SolidColorBrush.Parse("#172554"), // Blue subtle
                    "in_progress" => SolidColorBrush.Parse("#2E1065"), // Purple subtle
                    "completed" or "selesai" or "dikembalikan" => SolidColorBrush.Parse("#064E3B"), // Green subtle
                    "rejected" or "cancelled" => SolidColorBrush.Parse("#450A0A"), // Red subtle
                    _ => SolidColorBrush.Parse("#1E293B")
                };
            }
            return SolidColorBrush.Parse("#1E293B");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StatusBadgeFgConverter : IValueConverter
    {
        public static readonly StatusBadgeFgConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLowerInvariant() switch
                {
                    "pending" => SolidColorBrush.Parse("#F59E0B"), // Amber
                    "scheduled" => SolidColorBrush.Parse("#60A5FA"), // Blue
                    "in_progress" => SolidColorBrush.Parse("#C084FC"), // Purple
                    "completed" or "selesai" or "dikembalikan" => SolidColorBrush.Parse("#10B981"), // Green
                    "rejected" or "cancelled" => SolidColorBrush.Parse("#EF4444"), // Red
                    _ => SolidColorBrush.Parse("#94A3B8")
                };
            }
            return SolidColorBrush.Parse("#94A3B8");
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class CurrencyFormatConverter : IValueConverter
    {
        public static readonly CurrencyFormatConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            decimal amount = 0;
            if (value is decimal d) amount = d;
            else if (value is double dbl) amount = (decimal)dbl;
            else if (value is int i) amount = i;
            else if (value is string s && decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                amount = parsed;
            }

            var idCulture = new CultureInfo("id-ID");
            return amount.ToString("C0", idCulture);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class BoolInverterConverter : IValueConverter
    {
        public static readonly BoolInverterConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class StringNotNullOrEmptyToBoolConverter : IValueConverter
    {
        public static readonly StringNotNullOrEmptyToBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullToBoolConverter : IValueConverter
    {
        public static readonly NullToBoolConverter Instance = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
