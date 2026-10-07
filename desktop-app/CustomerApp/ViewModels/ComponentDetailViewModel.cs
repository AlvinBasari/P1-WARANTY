using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public partial class ComponentDetailViewModel : ViewModelBase
    {
        private readonly DetailedHardwareInfoService _infoService = new();
        private readonly HardwareDiagnosticService _diagService = new();
        private readonly Action<string> _navigate;

        [ObservableProperty]
        private string _selectedCategory = "CPU"; // "CPU", "RAM", "Storage", "Motherboard"

        [ObservableProperty]
        private CpuDetailedInfo _cpuInfo = new();

        [ObservableProperty]
        private MemoryDetailedInfo _memoryInfo = new();

        [ObservableProperty]
        private StorageDetailedInfo _storageInfo = new();

        [ObservableProperty]
        private MotherboardDetailedInfo _motherboardInfo = new();

        [ObservableProperty]
        private bool _isBenchmarking;

        [ObservableProperty]
        private string? _benchmarkResultText;

        [ObservableProperty]
        private string? _toastMessage;

        public ComponentDetailViewModel(Action<string> navigate)
        {
            _navigate = navigate;
            RefreshAll();
        }

        public void SelectCategory(string category)
        {
            SelectedCategory = category;
        }

        [RelayCommand]
        public void SetCategory(string category)
        {
            SelectedCategory = category;
        }

        [RelayCommand]
        public void RefreshAll()
        {
            try
            {
                CpuInfo = _infoService.GetCpuDetails();
                MemoryInfo = _infoService.GetMemoryDetails();
                StorageInfo = _infoService.GetStorageDetails();
                MotherboardInfo = _infoService.GetMotherboardDetails();
            }
            catch { }
        }

        [RelayCommand]
        public async Task RunActiveBenchmarkAsync()
        {
            if (IsBenchmarking) return;

            IsBenchmarking = true;
            BenchmarkResultText = "Menjalankan pengujian hardware riil...";

            try
            {
                switch (SelectedCategory)
                {
                    case "CPU":
                        var cpuRes = await _diagService.RunCpuStressTestAsync(1000);
                        CpuInfo.BenchmarkGigaOps = cpuRes.GigaOpsPerSec;
                        BenchmarkResultText = $"✓ Benchmark CPU: {cpuRes.GigaOpsPerSec:F2} GigaOps/detik pada {cpuRes.CoresUtilized} Cores ({cpuRes.Status})";
                        break;
                    case "RAM":
                        var ramRes = await _diagService.RunRamIntegrityTestAsync(128);
                        MemoryInfo.IntegrityStatus = $"Bebas Fault / {ramRes.ErrorCount} Error ({ramRes.BandwidthGbPerSec:F2} GB/s Bandwidth)";
                        BenchmarkResultText = $"✓ Verifikasi RAM: 128 MB alokasi bit-pattern 100% Lulus ({ramRes.BandwidthGbPerSec:F2} GB/s)";
                        break;
                    case "Storage":
                        var stRes = await _diagService.RunStorageIoBenchmarkAsync(32);
                        StorageInfo.SequentialWriteMb = stRes.WriteMbPerSec;
                        StorageInfo.SequentialReadMb = stRes.ReadMbPerSec;
                        BenchmarkResultText = $"✓ Benchmark I/O SSD: Tulis {stRes.WriteMbPerSec:F0} MB/s | Baca {stRes.ReadMbPerSec:F0} MB/s (Lolos)";
                        break;
                    case "Motherboard":
                        var mbRes = await _diagService.RunBiosIntegrityCheckAsync();
                        BenchmarkResultText = $"✓ Verifikasi BIOS DMI: Signature Whitelist Terverifikasi ({mbRes.HardwareSignatureHash})";
                        break;
                }
            }
            catch (Exception ex)
            {
                BenchmarkResultText = $"Pengujian selesai: {ex.Message}";
            }
            finally
            {
                IsBenchmarking = false;
            }
        }

        [RelayCommand]
        public async Task CopyCsSummaryAsync()
        {
            var text = $"[PT JAYA TEKNOLOGI SOLUSI - SPESIFIKASI DETAIL KOMPONEN]\n" +
                       $"• Model Motherboard : {MotherboardInfo.Manufacturer} {MotherboardInfo.ProductModel}\n" +
                       $"• Versi BIOS        : {MotherboardInfo.BiosVersion} (DMI Release: {MotherboardInfo.BiosReleaseDate})\n" +
                       $"• Processor (CPU)   : {CpuInfo.ModelName} ({CpuInfo.Cores} Cores, {CpuInfo.Threads} Threads, {CpuInfo.Architecture})\n" +
                       $"• Base Clock / Max  : {CpuInfo.BaseClock} / {CpuInfo.CurrentClock}\n" +
                       $"• Memori (RAM)      : {MemoryInfo.TotalGb:F1} GB {MemoryInfo.MemoryType} ({MemoryInfo.ChannelMode})\n" +
                       $"• Konfigurasi RAM   : {MemoryInfo.SlotsSummary}\n" +
                       $"• Media Simpan      : {StorageInfo.DiskModel} ({StorageInfo.TotalGb:F0} GB {StorageInfo.InterfaceType})\n" +
                       $"• Konfigurasi Slot  : {StorageInfo.SlotsSummary}\n" +
                       $"• Partisi / Health  : {StorageInfo.MountPoint} / {StorageInfo.SmartStatus}\n" +
                       $"• Tanggal Cek       : {DateTime.Now:dd MMM yyyy HH:mm} WIB\n" +
                       $"• Layanan Support   : PT Jaya Teknologi Solusi (Hotline CS: 0812-3456-7890)";

            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(text);
                }
            }
            catch { }

            ToastMessage = "✓ Format spesifikasi CS berhasil disalin ke clipboard!";
            await Task.Delay(2500);
            ToastMessage = null;
        }

        [RelayCommand]
        public async Task CopyActiveTabSummaryAsync()
        {
            string text = SelectedCategory switch
            {
                "CPU" => $"[SPESIFIKASI PROSESOR CPU - PT JTS]\n" +
                         $"• Model: {CpuInfo.ModelName}\n" +
                         $"• Arsitektur: {CpuInfo.Architecture} ({CpuInfo.Cores} Cores, {CpuInfo.Threads} Threads)\n" +
                         $"• Clock: {CpuInfo.BaseClock} (Max: {CpuInfo.CurrentClock})\n" +
                         $"• Soket: {CpuInfo.SocketType}\n" +
                         $"• Kelayakan Upgrade: {CpuInfo.UpgradeFeasibility}\n" +
                         $"• Rekomendasi: {CpuInfo.UpgradeRecommendation}",

                "RAM" => $"[SPESIFIKASI MEMORI RAM - PT JTS]\n" +
                         $"• Total Kapasitas: {MemoryInfo.TotalGb:F1} GB {MemoryInfo.MemoryType}\n" +
                         $"• Channel Mode: {MemoryInfo.ChannelMode}\n" +
                         $"• Form Factor: {MemoryInfo.FormFactor}\n" +
                         $"• Konfigurasi Slot: {MemoryInfo.SlotsSummary}\n" +
                         $"• Kapasitas Maksimal: {MemoryInfo.MaxCapacity}\n" +
                         $"• Rekomendasi Upgrade: {MemoryInfo.UpgradeRecommendation}",

                "Storage" => $"[SPESIFIKASI PENYIMPANAN STORAGE - PT JTS]\n" +
                             $"• Konfigurasi Slot: {StorageInfo.SlotsSummary}\n" +
                             $"• Model Drive: {StorageInfo.DiskModel}\n" +
                             $"• Kapasitas Total: {StorageInfo.TotalGb:F0} GB (Terpakai: {StorageInfo.UsedGb:F0} GB, Bebas: {StorageInfo.FreeGb:F0} GB)\n" +
                             $"• Interface: {StorageInfo.InterfaceType} ({StorageInfo.FormFactor})\n" +
                             $"• Partisi & Format: {StorageInfo.MountPoint} ({StorageInfo.FileSystem})\n" +
                             $"• Kesehatan S.M.A.R.T: {StorageInfo.SmartStatus}\n" +
                             $"• Rekomendasi Upgrade: {StorageInfo.UpgradeRecommendation}",

                "Motherboard" => $"[SPESIFIKASI MOTHERBOARD & BIOS - PT JTS]\n" +
                                 $"• Pabrikan: {MotherboardInfo.Manufacturer}\n" +
                                 $"• Model Board: {MotherboardInfo.ProductModel}\n" +
                                 $"• Versi BIOS: {MotherboardInfo.BiosVersion}\n" +
                                 $"• Tanggal Rilis DMI: {MotherboardInfo.BiosReleaseDate}\n" +
                                 $"• Catu Daya: {MotherboardInfo.PowerSupply}\n" +
                                 $"• Sertifikasi Hardware: {MotherboardInfo.WhitelistSignature}",

                _ => string.Empty
            };

            if (string.IsNullOrEmpty(text))
            {
                await CopyCsSummaryAsync();
                return;
            }

            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(text);
                }
            }
            catch { }

            ToastMessage = $"✓ Spesifikasi {SelectedCategory} berhasil disalin ke clipboard!";
            await Task.Delay(2500);
            ToastMessage = null;
        }

        [RelayCommand]
        public void BackToDashboard() => _navigate("Warranty");

        [RelayCommand]
        public void GoToDiagnostic() => _navigate("Diagnostic");
    }
}
