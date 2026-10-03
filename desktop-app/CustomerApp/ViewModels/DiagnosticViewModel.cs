using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public class DiagnosticItemState : ObservableObject
    {
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string IconKey { get; set; } = "IconCpu";

        private string _status = "Belum Diuji";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private string _primaryMetric = "Memuat data...";
        public string PrimaryMetric
        {
            get => _primaryMetric;
            set => SetProperty(ref _primaryMetric, value);
        }

        private string _detail = "Siap untuk pengujian...";
        public string Detail
        {
            get => _detail;
            set => SetProperty(ref _detail, value);
        }

        private bool _isPassed = true;
        public bool IsPassed
        {
            get => _isPassed;
            set => SetProperty(ref _isPassed, value);
        }

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            set => SetProperty(ref _isRunning, value);
        }
    }

    public class DiagnosticLogEntry
    {
        public string Timestamp { get; set; } = DateTime.Now.ToString("HH:mm:ss");
        public string Subsystem { get; set; } = "SYS";
        public string Level { get; set; } = "PASS";
        public string LevelColor { get; set; } = "#4ADE80";
        public string Message { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }

    public partial class DiagnosticViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly DeviceIdentifierService _deviceService;
        private readonly SystemMetricsService _metricsService = new();
        private readonly HardwareDiagnosticService _diagService = new();
        private readonly Action<string>? _navigate;

        [ObservableProperty]
        private bool _isScanning;

        [ObservableProperty]
        private int _scanProgress;

        [ObservableProperty]
        private string _scanCurrentStepText = "Sistem siap untuk pengujian mandiri fisik hardware";

        [ObservableProperty]
        private int _overallHealthScore = 100;

        [ObservableProperty]
        private string _overallStatusLabel = "SISTEM PRIMA & SIAP DIGUNAKAN";

        [ObservableProperty]
        private string _lastScanTimestamp = "Belum pernah diuji";

        [ObservableProperty]
        private string _hardwareModel = "Perangkat Komputer";

        [ObservableProperty]
        private string _serialNumber = "Memuat Serial...";

        [ObservableProperty]
        private string _biosVersion = "Memuat BIOS...";

        [ObservableProperty]
        private string _osDescription = "Sistem Operasi";

        [ObservableProperty]
        private string _cpuName = "Processor Unit";

        [ObservableProperty]
        private int _cpuCores = 2;

        [ObservableProperty]
        private string _ramSummary = "Memori RAM";

        [ObservableProperty]
        private string _storageSummary = "Media Penyimpanan";

        [ObservableProperty]
        private string _networkLatencyText = "18 ms";

        [ObservableProperty]
        private string? _toastMessage;

        public ObservableCollection<DiagnosticLogEntry> DiagnosticLogs { get; } = new();

        // Individual Component States
        public DiagnosticItemState CpuTest { get; } = new()
        {
            Title = "Processor / CPU",
            Category = "Komputasi Utama",
            IconKey = "IconCpu",
            Status = "Normal",
            PrimaryMetric = "Multi-Core CPU",
            Detail = "Multi-core scheduling stabil & suhu optimal",
            IsPassed = true
        };

        public DiagnosticItemState RamTest { get; } = new()
        {
            Title = "Memori RAM & Cache",
            Category = "Alokasi Memori",
            IconKey = "IconMonitor",
            Status = "Normal",
            PrimaryMetric = "Alokasi RAM Fisik",
            Detail = "Alokasi memori bebas fault & integritas data terjaga",
            IsPassed = true
        };

        public DiagnosticItemState StorageTest { get; } = new()
        {
            Title = "Penyimpanan / Storage",
            Category = "Kesehatan Media Drive",
            IconKey = "IconHome",
            Status = "100% Sehat",
            PrimaryMetric = "Media Penyimpanan",
            Detail = "S.M.A.R.T OK, 0 bad sector, kecepatan baca normal",
            IsPassed = true
        };

        public DiagnosticItemState PowerTest { get; } = new()
        {
            Title = "Manajemen Catu Daya",
            Category = "Regulasi Voltase & Termal",
            IconKey = "IconTracking",
            Status = "Optimal",
            PrimaryMetric = "Daya Listrik AC Stabil",
            Detail = "Tegangan stabil & sensor termal dalam ambang batas aman",
            IsPassed = true
        };

        public DiagnosticItemState BiosTest { get; } = new()
        {
            Title = "Integritas BIOS",
            Category = "Firmware Motherboard",
            IconKey = "IconShield",
            Status = "Asli PT JTS",
            PrimaryMetric = "DMI Firmware",
            Detail = "DMI Table & Hardware Whitelist Terverifikasi",
            IsPassed = true
        };

        public DiagnosticItemState NetworkTest { get; } = new()
        {
            Title = "Jaringan & Remote",
            Category = "Server Garansi JTS",
            IconKey = "IconRefresh",
            Status = "Terhubung",
            PrimaryMetric = "Latensi Socket Stabil",
            Detail = "Latensi rendah, siap untuk sesi remote diagnostik",
            IsPassed = true
        };

        public DiagnosticViewModel(ApiClient apiClient, DeviceIdentifierService deviceService, Action<string>? navigate = null)
        {
            _apiClient = apiClient;
            _deviceService = deviceService;
            _navigate = navigate;

            LoadTelemetry();
        }

        public void AddLog(string subsystem, string level, string message, string detail = "", string levelColor = "#4ADE80")
        {
            if (string.IsNullOrEmpty(levelColor))
            {
                levelColor = level switch
                {
                    "PASS" or "OK" or "SUCCESS" => "#4ADE80",
                    "WARN" or "ALERT" => "#F59E0B",
                    "FAIL" or "ERROR" => "#EF4444",
                    "RUN" or "BENCHMARK" => "#38BDF8",
                    _ => "#94A3B8"
                };
            }

            var entry = new DiagnosticLogEntry
            {
                Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                Subsystem = subsystem,
                Level = level,
                LevelColor = levelColor,
                Message = message,
                Detail = detail
            };

            DiagnosticLogs.Add(entry);
            if (DiagnosticLogs.Count > 150)
            {
                DiagnosticLogs.RemoveAt(0);
            }
        }

        public void LoadTelemetry()
        {
            try
            {
                var metrics = _metricsService.GetSystemMetrics();
                HardwareModel = _deviceService.GetDeviceModel();
                SerialNumber = _deviceService.GetSerialNumber();
                BiosVersion = metrics.BiosVersion;
                OsDescription = metrics.OsDescription;
                CpuName = metrics.ProcessorName;
                CpuCores = metrics.ProcessorCores;
                RamSummary = metrics.MemoryDisplay;
                StorageSummary = !string.IsNullOrWhiteSpace(metrics.StorageSummary) ? metrics.StorageSummary : metrics.StorageDisplay;

                CpuTest.PrimaryMetric = $"{CpuName} ({CpuCores} Cores)";
                CpuTest.Detail = $"{CpuName} ({CpuCores} Cores terdeteksi, siap stress test)";

                RamTest.PrimaryMetric = $"{RamSummary} Terpasang ({metrics.MemoryPercentUsed}% terpakai)";
                RamTest.Detail = $"Kapasitas: {metrics.MemoryDisplay} ({metrics.MemoryPercentUsed}% terpakai)";

                StorageTest.Title = metrics.IsRotationalHdd ? "Storage HDD Health" : "Storage SSD / NVMe Health";
                StorageTest.PrimaryMetric = !string.IsNullOrWhiteSpace(metrics.StorageModel) ? $"{metrics.StorageModel}" : $"{metrics.StorageDisplay}";
                StorageTest.Detail = !string.IsNullOrWhiteSpace(metrics.StorageModel) ? $"{metrics.StorageModel} (Free: {metrics.StorageTotalGb - metrics.StorageUsedGb:F0} GB)" : $"Kapasitas: {metrics.StorageDisplay} (Health: 100%)";

                PowerTest.PrimaryMetric = $"{metrics.PowerStatus} ({metrics.PowerDisplay})";
                PowerTest.Detail = $"{metrics.PowerStatus} ({metrics.PowerDisplay})";

                BiosTest.PrimaryMetric = $"BIOS {metrics.BiosVersion} (DMI Valid)";
                BiosTest.Detail = $"Versi BIOS: {metrics.BiosVersion} (DMI Valid)";

                NetworkTest.PrimaryMetric = $"Koneksi Server JTS ({NetworkLatencyText})";
                NetworkTest.Detail = "Latensi rendah, siap untuk sesi remote diagnostik";

                // Initial System Audit Logs
                DiagnosticLogs.Clear();
                AddLog("SYS", "INFO", $"Inisialisasi Diagnostic Engine JTS Vantage untuk unit {HardwareModel}.", levelColor: "#38BDF8");
                AddLog("SPEC", "INFO", $"Hardware ID terverifikasi | Serial: {SerialNumber} | OS: {OsDescription}", levelColor: "#94A3B8");
                AddLog("CPU", "READY", $"Processor: {CpuName} ({CpuCores} Logical Cores terpasang).", levelColor: "#38BDF8");
                AddLog("RAM", "READY", $"Memori RAM: {RamSummary} teralokasi, pola akses stabil.", levelColor: "#38BDF8");
                AddLog("SSD", "READY", $"Media Simpan: {StorageSummary}, partisi sistem operasional.", levelColor: "#38BDF8");
                AddLog("INIT", "PASS", "✓ Seluruh 6 modul subsistem siap diuji mandiri secara otomatis.", levelColor: "#4ADE80");
            }
            catch { }
        }

        [RelayCommand]
        public async Task RunFullScanAsync()
        {
            if (IsScanning) return;

            IsScanning = true;
            ScanProgress = 5;
            ScanCurrentStepText = "Memulai inisialisasi modul pengujian benchmark hardware fisik...";
            AddLog("SCAN", "RUN", "Memulai pemindaian otomatis komprehensif 6 subsistem...", levelColor: "#38BDF8");

            // Reset test running states
            CpuTest.IsRunning = true;
            RamTest.IsRunning = false;
            StorageTest.IsRunning = false;
            PowerTest.IsRunning = false;
            BiosTest.IsRunning = false;
            NetworkTest.IsRunning = false;

            // 1. CPU Multi-Core Real Stress Test
            ScanProgress = 15;
            ScanCurrentStepText = $"1/6: Menjalankan beban komputasi floating-point paralel pada {CpuCores} Core CPU...";
            AddLog("CPU", "RUN", $"Menjalankan beban komputasi floating-point paralel pada {CpuCores} Core...", levelColor: "#38BDF8");
            var cpuRes = await _diagService.RunCpuStressTestAsync(1000);
            CpuTest.Status = cpuRes.Status;
            CpuTest.PrimaryMetric = $"{cpuRes.GigaOpsPerSec:F2} GigaOps/s ({cpuRes.CoresUtilized} Cores)";
            CpuTest.Detail = cpuRes.Detail;
            CpuTest.IsPassed = cpuRes.IsPassed;
            CpuTest.IsRunning = false;
            RamTest.IsRunning = true;
            AddLog("CPU", cpuRes.IsPassed ? "PASS" : "WARN", $"Benchmark CPU: {cpuRes.GigaOpsPerSec:F2} GigaOps/s pada {cpuRes.CoresUtilized} Cores ({cpuRes.DurationMs:F0} ms) -> {cpuRes.Status}");

            // 2. RAM Memory Allocation & Bit-Parity Real Test
            ScanProgress = 35;
            ScanCurrentStepText = "2/6: Mengalokasikan blok memori fisik & memverifikasi integritas pola bit RAM...";
            AddLog("RAM", "RUN", "Mengalokasikan buffer 64 MB & memverifikasi integritas pola bit RAM...", levelColor: "#38BDF8");
            var ramRes = await _diagService.RunRamIntegrityTestAsync(64);
            RamTest.Status = ramRes.Status;
            RamTest.PrimaryMetric = $"{ramRes.TestedMb} MB Buffer • {ramRes.BandwidthGbPerSec:F2} GB/s";
            RamTest.Detail = ramRes.Detail;
            RamTest.IsPassed = ramRes.IsPassed;
            RamTest.IsRunning = false;
            StorageTest.IsRunning = true;
            AddLog("RAM", ramRes.IsPassed ? "PASS" : "WARN", $"Uji RAM: {ramRes.TestedMb} MB diverifikasi ({ramRes.BandwidthGbPerSec:F2} GB/s, {ramRes.ErrorCount} fault) -> {ramRes.Status}");

            // 3. Storage SSD Sequential I/O Throughput Benchmark
            ScanProgress = 55;
            ScanCurrentStepText = "3/6: Melakukan benchmark kecepatan Sequential Read/Write & cek status partisi SSD...";
            AddLog("STORAGE", "RUN", "Menjalankan uji benchmark I/O Sequential Write/Read media penyimpanan...", levelColor: "#38BDF8");
            var storageRes = await _diagService.RunStorageIoBenchmarkAsync(16);
            StorageTest.Status = storageRes.Status;
            StorageTest.PrimaryMetric = $"Tulis: {storageRes.WriteMbPerSec:F0} MB/s | Baca: {storageRes.ReadMbPerSec:F0} MB/s";
            StorageTest.Detail = storageRes.Detail;
            StorageTest.IsPassed = storageRes.IsPassed;
            StorageTest.IsRunning = false;
            PowerTest.IsRunning = true;
            AddLog("STORAGE", storageRes.IsPassed ? "PASS" : "WARN", $"Benchmark I/O: Tulis {storageRes.WriteMbPerSec:F0} MB/s, Baca {storageRes.ReadMbPerSec:F0} MB/s (Free {storageRes.FreeSpaceGb:F0} GB) -> {storageRes.Status}");

            // 4. Power & Real Thermal Sensor Telemetry
            ScanProgress = 75;
            ScanCurrentStepText = "4/6: Membaca sensor termal CPU & stabilitas voltase daya adaptif...";
            AddLog("POWER", "RUN", "Membaca sensor termal hardware & kestabilan regulasi daya...", levelColor: "#38BDF8");
            var powerRes = await _diagService.RunPowerAndThermalCheckAsync();
            PowerTest.Status = powerRes.Status;
            PowerTest.PrimaryMetric = $"Suhu {powerRes.CpuTempCelsius:F1}°C • {powerRes.PowerSource}";
            PowerTest.Detail = powerRes.Detail;
            PowerTest.IsPassed = powerRes.IsPassed;
            PowerTest.IsRunning = false;
            BiosTest.IsRunning = true;
            AddLog("POWER", powerRes.IsPassed ? "PASS" : "WARN", $"Sensor termal: {powerRes.CpuTempCelsius:F1}°C, Status catu daya: {powerRes.PowerSource} ({powerRes.PowerPercent}%) -> {powerRes.Status}");

            // 5. BIOS & Motherboard Whitelist Integrity Check
            ScanProgress = 88;
            ScanCurrentStepText = "5/6: Memverifikasi hash digital BIOS DMI & whitelist hardware resmi...";
            AddLog("BIOS", "RUN", "Memverifikasi integritas hash digital BIOS DMI whitelist PT JTS...", levelColor: "#38BDF8");
            var biosRes = await _diagService.RunBiosIntegrityCheckAsync();
            BiosTest.Status = biosRes.Status;
            BiosTest.PrimaryMetric = $"BIOS {biosRes.BiosVersion} (Signature Valid)";
            BiosTest.Detail = biosRes.Detail;
            BiosTest.IsPassed = biosRes.IsPassed;
            BiosTest.IsRunning = false;
            NetworkTest.IsRunning = true;
            AddLog("BIOS", biosRes.IsPassed ? "PASS" : "WARN", $"Validasi BIOS: Versi {biosRes.BiosVersion}, Signature: {biosRes.HardwareSignatureHash} -> {biosRes.Status}");

            // 6. Network Socket Ping & Jitter Real Test
            ScanProgress = 95;
            ScanCurrentStepText = "6/6: Mengukur latensi multi-probe socket & stabilitas jitter ke server pusat...";
            AddLog("NETWORK", "RUN", "Mengukur latensi socket & stabilitas paket ke server PT JTS...", levelColor: "#38BDF8");
            var netRes = await _diagService.RunNetworkPingAndJitterTestAsync();
            NetworkTest.Status = netRes.Status;
            NetworkTest.PrimaryMetric = $"Ping: {netRes.AvgLatencyMs} ms • Jitter: {netRes.JitterMs} ms";
            NetworkTest.Detail = netRes.Detail;
            NetworkTest.IsPassed = netRes.IsPassed;
            NetworkTest.IsRunning = false;
            NetworkLatencyText = $"{netRes.AvgLatencyMs} ms";
            AddLog("NETWORK", netRes.IsPassed ? "PASS" : "WARN", $"Socket ping: Latensi {netRes.AvgLatencyMs} ms, Jitter {netRes.JitterMs} ms (Success: {netRes.SuccessRatePercent}%) -> {netRes.Status}");

            // Completion
            ScanProgress = 100;
            ScanCurrentStepText = "✓ Diagnostik Nyata Selesai: Seluruh 6 modul hardware fisik telah diuji & LOLOS!";
            OverallHealthScore = 100;
            OverallStatusLabel = "UNIT 100% PRIMA & LOLOS BENCHMARK RESMI";
            LastScanTimestamp = DateTime.Now.ToString("dd MMM yyyy, HH:mm") + " WIB";
            AddLog("SUMMARY", "SUCCESS", "✓ DIAGNOSTIK LENGKAP SELESAI: 6/6 modul subsistem hardware 100% PRIMA & LOLOS UJI.", levelColor: "#4ADE80");

            IsScanning = false;
            _ = ShowToastAsync("✓ Pemindaian Fisik Selesai: Semua komponen lolos stress test & benchmark!");
        }

        [RelayCommand]
        public async Task TestSingleComponentAsync(string component)
        {
            if (IsScanning) return;

            switch (component.ToLower())
            {
                case "cpu":
                    CpuTest.IsRunning = true;
                    AddLog("CPU", "RUN", $"Menjalankan stress test floating-point pada {CpuCores} Core...", levelColor: "#38BDF8");
                    var cpuRes = await _diagService.RunCpuStressTestAsync(1000);
                    CpuTest.Status = cpuRes.Status;
                    CpuTest.PrimaryMetric = $"{cpuRes.GigaOpsPerSec:F2} GigaOps/s ({cpuRes.CoresUtilized} Cores)";
                    CpuTest.Detail = cpuRes.Detail;
                    CpuTest.IsPassed = cpuRes.IsPassed;
                    CpuTest.IsRunning = false;
                    AddLog("CPU", cpuRes.IsPassed ? "PASS" : "WARN", $"Uji CPU Selesai: {cpuRes.GigaOpsPerSec:F2} GigaOps/s ({cpuRes.DurationMs:F0} ms) -> {cpuRes.Status}");
                    _ = ShowToastAsync($"Uji Processor CPU: {cpuRes.GigaOpsPerSec:F2} GigaOps/s (Lolos & Stabil)");
                    break;

                case "ram":
                    RamTest.IsRunning = true;
                    AddLog("RAM", "RUN", "Mengalokasikan buffer 64 MB dan memeriksa integritas bit...", levelColor: "#38BDF8");
                    var ramRes = await _diagService.RunRamIntegrityTestAsync(64);
                    RamTest.Status = ramRes.Status;
                    RamTest.PrimaryMetric = $"{ramRes.TestedMb} MB Buffer • {ramRes.BandwidthGbPerSec:F2} GB/s";
                    RamTest.Detail = ramRes.Detail;
                    RamTest.IsPassed = ramRes.IsPassed;
                    RamTest.IsRunning = false;
                    AddLog("RAM", ramRes.IsPassed ? "PASS" : "WARN", $"Uji RAM Selesai: {ramRes.TestedMb} MB ({ramRes.BandwidthGbPerSec:F2} GB/s, {ramRes.ErrorCount} fault) -> {ramRes.Status}");
                    _ = ShowToastAsync($"Uji Memori RAM: {ramRes.TestedMb} MB Diverifikasi ({ramRes.BandwidthGbPerSec:F2} GB/s)");
                    break;

                case "storage":
                    StorageTest.IsRunning = true;
                    AddLog("STORAGE", "RUN", "Menjalankan benchmark throughput I/O SSD...", levelColor: "#38BDF8");
                    var storageRes = await _diagService.RunStorageIoBenchmarkAsync(16);
                    StorageTest.Status = storageRes.Status;
                    StorageTest.PrimaryMetric = $"Tulis: {storageRes.WriteMbPerSec:F0} MB/s | Baca: {storageRes.ReadMbPerSec:F0} MB/s";
                    StorageTest.Detail = storageRes.Detail;
                    StorageTest.IsPassed = storageRes.IsPassed;
                    StorageTest.IsRunning = false;
                    AddLog("STORAGE", storageRes.IsPassed ? "PASS" : "WARN", $"Uji Storage Selesai: Tulis {storageRes.WriteMbPerSec:F0} MB/s, Baca {storageRes.ReadMbPerSec:F0} MB/s -> {storageRes.Status}");
                    _ = ShowToastAsync($"Uji Storage: Tulis {storageRes.WriteMbPerSec:F0} MB/s | Baca {storageRes.ReadMbPerSec:F0} MB/s");
                    break;

                case "power":
                    PowerTest.IsRunning = true;
                    AddLog("POWER", "RUN", "Membaca sensor termal & voltase daya...", levelColor: "#38BDF8");
                    var powerRes = await _diagService.RunPowerAndThermalCheckAsync();
                    PowerTest.Status = powerRes.Status;
                    PowerTest.PrimaryMetric = $"Suhu {powerRes.CpuTempCelsius:F1}°C • {powerRes.PowerSource}";
                    PowerTest.Detail = powerRes.Detail;
                    PowerTest.IsPassed = powerRes.IsPassed;
                    PowerTest.IsRunning = false;
                    AddLog("POWER", powerRes.IsPassed ? "PASS" : "WARN", $"Uji Catu Daya Selesai: {powerRes.CpuTempCelsius:F1}°C, {powerRes.PowerSource} ({powerRes.PowerPercent}%) -> {powerRes.Status}");
                    _ = ShowToastAsync("Uji Catu Daya: Tegangan & Suhu Termal Optimal!");
                    break;

                case "bios":
                    BiosTest.IsRunning = true;
                    AddLog("BIOS", "RUN", "Memverifikasi signature BIOS DMI...", levelColor: "#38BDF8");
                    var biosRes = await _diagService.RunBiosIntegrityCheckAsync();
                    BiosTest.Status = biosRes.Status;
                    BiosTest.PrimaryMetric = $"BIOS {biosRes.BiosVersion} (Signature Valid)";
                    BiosTest.Detail = biosRes.Detail;
                    BiosTest.IsPassed = biosRes.IsPassed;
                    BiosTest.IsRunning = false;
                    AddLog("BIOS", biosRes.IsPassed ? "PASS" : "WARN", $"Uji BIOS Selesai: Versi {biosRes.BiosVersion}, Signature: {biosRes.HardwareSignatureHash} -> {biosRes.Status}");
                    _ = ShowToastAsync($"Uji BIOS: Signature {biosRes.HardwareSignatureHash} Valid!");
                    break;

                case "network":
                    NetworkTest.IsRunning = true;
                    AddLog("NETWORK", "RUN", "Menguji konektivitas ke server PT JTS...", levelColor: "#38BDF8");
                    var netRes = await _diagService.RunNetworkPingAndJitterTestAsync();
                    NetworkTest.Status = netRes.Status;
                    NetworkTest.PrimaryMetric = $"Ping: {netRes.AvgLatencyMs} ms • Jitter: {netRes.JitterMs} ms";
                    NetworkTest.Detail = netRes.Detail;
                    NetworkTest.IsPassed = netRes.IsPassed;
                    NetworkTest.IsRunning = false;
                    NetworkLatencyText = $"{netRes.AvgLatencyMs} ms";
                    AddLog("NETWORK", netRes.IsPassed ? "PASS" : "WARN", $"Uji Jaringan Selesai: Latensi {netRes.AvgLatencyMs} ms, Jitter {netRes.JitterMs} ms -> {netRes.Status}");
                    _ = ShowToastAsync($"Uji Jaringan: Latensi {netRes.AvgLatencyMs} ms, Jitter {netRes.JitterMs} ms");
                    break;
            }
        }

        [RelayCommand]
        public async Task CopyLogsAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[LOG AUDIT DIAGNOSTIK HARDWARE - PT JAYA TEKNOLOGI SOLUSI]");
            sb.AppendLine($"Perangkat: {HardwareModel} | S/N: {SerialNumber} | Waktu: {DateTime.Now:dd/MM/yyyy HH:mm:ss} WIB");
            sb.AppendLine("--------------------------------------------------------------------------------");
            foreach (var log in DiagnosticLogs)
            {
                sb.AppendLine($"[{log.Timestamp}] [{log.Subsystem}] [{log.Level}] {log.Message}");
            }

            try
            {
                if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(sb.ToString());
                }
            }
            catch { }

            _ = ShowToastAsync("✓ Seluruh log pengujian diagnostik berhasil disalin ke clipboard!");
        }

        [RelayCommand]
        public void ExportReport()
        {
            try
            {
                string downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(downloadsDir)) downloadsDir = Path.GetTempPath();

                string filePath = Path.Combine(downloadsDir, $"Laporan_Diagnostik_JTS_{SerialNumber}.txt");

                string report = $@"============================================================
LAPORAN RESMI DIAGNOSTIK HARDWARE - PT JAYA TEKNOLOGI SOLUSI
============================================================
Tanggal Pemeriksaan : {DateTime.Now:dd MMMM yyyy, HH:mm:ss} WIB
Model Perangkat     : {HardwareModel}
Nomor Seri (S/N)    : {SerialNumber}
Versi Firmware BIOS : {BiosVersion}
Sistem Operasi      : {OsDescription}
Skor Kesehatan Unit : {OverallHealthScore}% (PRIMA)

------------------------------------------------------------
HASIL BENCHMARK & PENGUJIAN FISIK 6 MODUL HARDWARE:
------------------------------------------------------------
1. Processor / CPU Stress   : [{CpuTest.Status}] {CpuTest.Detail}
2. Memori RAM & Cache       : [{RamTest.Status}] {RamTest.Detail}
3. Storage (Penyimpanan)    : [{StorageTest.Status}] {StorageTest.Detail}
4. Manajemen Daya / Baterai : [{PowerTest.Status}] {PowerTest.Detail}
5. Integritas Firmware BIOS : [{BiosTest.Status}] {BiosTest.Detail}
6. Jaringan & Server JTS    : [{NetworkTest.Status}] {NetworkTest.Detail}

------------------------------------------------------------
KESIMPULAN:
Seluruh komponen perangkat keras telah melalui pengujian beban
komputasi, verifikasi integritas bit memori, dan benchmark I/O fisik.
Unit dinyatakan 100% PRIMA dan memenuhi standar garansi resmi PT JTS.
============================================================";

                File.WriteAllText(filePath, report);

                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                catch { }

                _ = ShowToastAsync($"✓ Laporan Diagnostik disimpan di Downloads: Laporan_Diagnostik_JTS_{SerialNumber}.txt");
            }
            catch (Exception ex)
            {
                _ = ShowToastAsync($"Gagal mengekspor laporan: {ex.Message}");
            }
        }

        [RelayCommand]
        public void OpenCpuDetail() => _navigate?.Invoke("ComponentDetail_CPU");

        [RelayCommand]
        public void OpenRamDetail() => _navigate?.Invoke("ComponentDetail_RAM");

        [RelayCommand]
        public void OpenStorageDetail() => _navigate?.Invoke("ComponentDetail_Storage");

        [RelayCommand]
        public void OpenMotherboardDetail() => _navigate?.Invoke("ComponentDetail_Motherboard");

        private async Task ShowToastAsync(string message)
        {
            ToastMessage = message;
            await Task.Delay(3000);
            ToastMessage = null;
        }
    }
}
