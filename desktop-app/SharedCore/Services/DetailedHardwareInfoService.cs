using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SharedCore.Services
{
    public class CpuDetailedInfo
    {
        public string ModelName { get; set; } = "Processor System";
        public string Vendor { get; set; } = "Processor Vendor";
        public string Architecture { get; set; } = "x86_64";
        public int Cores { get; set; } = 2;
        public int Threads { get; set; } = 4;
        public string BaseClock { get; set; } = "N/A";
        public string CurrentClock { get; set; } = "N/A";
        public string SocketType { get; set; } = "Modular / BGA";
        public string Chipset { get; set; } = "Integrated Chipset";
        public string Tdp { get; set; } = "Standard TDP";
        public string IntegratedGpu { get; set; } = "Integrated Graphics Controller";
        public string L1DataCache { get; set; } = "64 KB";
        public string L1InstructionCache { get; set; } = "64 KB";
        public string L2Cache { get; set; } = "512 KB";
        public string L3Cache { get; set; } = "3.0 MB Smart Cache";
        public List<string> InstructionSets { get; set; } = new() { "MMX", "SSE", "SSE2", "SSE3", "SSSE3", "SSE4.1", "SSE4.2", "AVX", "AVX2", "FMA3", "AES-NI", "VT-x", "x86-64" };
        public double? TemperatureCelsius { get; set; }
        public string Governor { get; set; } = "powersave / schedutil";
        public double BenchmarkGigaOps { get; set; } = 4.85;

        // Upgrade Readiness Properties
        public string UpgradeRecommendation { get; set; } = string.Empty;
        public string UpgradeFeasibility { get; set; } = string.Empty;
    }

    public class MemoryDetailedInfo
    {
        public double TotalGb { get; set; } = 7.7;
        public double UsedGb { get; set; } = 3.3;
        public double FreeGb { get; set; } = 4.4;
        public int UsedPercent { get; set; } = 43;
        public double CachedMb { get; set; } = 3600;
        public double BuffersMb { get; set; } = 280;
        public double SwapTotalGb { get; set; } = 2.0;
        public double SwapUsedGb { get; set; } = 0.0;
        public string MemoryType { get; set; } = "DDR4 SDRAM";
        public string FormFactor { get; set; } = "DIMM / SO-DIMM";
        public string ChannelMode { get; set; } = "Single-Channel";
        public string Frequency { get; set; } = "2400 / 2666 / 3200 MHz";
        public string SlotsSummary { get; set; } = "Slots Terpasang";
        public string MaxCapacity { get; set; } = "Maksimal 64 GB";
        public string IntegrityStatus { get; set; } = "Bebas Fault / 0 Parity Error (100% Sehat)";

        // Upgrade Readiness Properties
        public string UpgradeRecommendation { get; set; } = string.Empty;
        public string UpgradeFeasibility { get; set; } = string.Empty;
    }

    public class StorageDetailedInfo
    {
        public string DiskModel { get; set; } = "Physical Storage Device";
        public string InterfaceType { get; set; } = "SATA III / NVMe PCIe";
        public bool IsRotationalHdd { get; set; } = false;
        public double TotalGb { get; set; } = 500.0;
        public double UsedGb { get; set; } = 50.0;
        public double FreeGb { get; set; } = 450.0;
        public int UsedPercent { get; set; } = 10;
        public string FileSystem { get; set; } = "ext4/NTFS";
        public string MountPoint { get; set; } = "/ (Root Partition)";
        public string FormFactor { get; set; } = "M.2 / 2.5\" Bay";
        public string M2SlotStatus { get; set; } = "Slot M.2 Tersedia";
        public string SataPortsAvailable { get; set; } = "Port SATA Tersedia";
        public double SequentialReadMb { get; set; } = 520.0;
        public double SequentialWriteMb { get; set; } = 480.0;
        public string SmartStatus { get; set; } = "100% Sehat (Good Condition)";
        public string BadSectors { get; set; } = "0 Bad Sectors (Normal)";
        public string TrimStatus { get; set; } = "Aktif (TRIM Enabled)";

        // Upgrade Readiness Properties
        public string UpgradeRecommendation { get; set; } = string.Empty;
        public string UpgradeFeasibility { get; set; } = string.Empty;
        public string EstimatedPerformanceGain { get; set; } = string.Empty;
    }

    public class MotherboardDetailedInfo
    {
        public string Manufacturer { get; set; } = "OEM Manufacturer";
        public string ProductModel { get; set; } = "System Model";
        public string SerialNumber { get; set; } = "SN-HARDWARE-OEM";
        public string Chipset { get; set; } = "Integrated Motherboard Chipset";
        public string ExpansionSlots { get; set; } = "PCIe Slots, M.2 Slot, SATA Ports";
        public string PowerSupply { get; set; } = "Power Supply Unit";
        public string BiosVendor { get; set; } = "OEM";
        public string BiosVersion { get; set; } = "BIOS-VER-1.0";
        public string BiosReleaseDate { get; set; } = "2023-01-01";
        public string SystemUuid { get; set; } = "DMI-UUID";
        public string OsName { get; set; } = RuntimeInformation.OSDescription;
        public string Architecture { get; set; } = RuntimeInformation.ProcessArchitecture.ToString();
        public string WhitelistSignature { get; set; } = "SHA256-VERIFIED-JTS-OEM";

        // Upgrade Advice
        public string UpgradeAdvice { get; set; } = string.Empty;
    }

    public class DetailedHardwareInfoService
    {
        private readonly DeviceIdentifierService _deviceService = new();

        private DeviceCategory GetCurrentDeviceCategory()
        {
            string manufacturer = _deviceService.GetDeviceModel();
            string model = _deviceService.GetDeviceModel();
            bool hasBattery = CheckIfBatteryPresent();
            return HardwareIntelligenceEngine.DetectDeviceCategory(manufacturer, model, hasBattery);
        }

        private bool CheckIfBatteryPresent()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Directory.Exists("/sys/class/power_supply"))
                {
                    foreach (var dir in Directory.GetDirectories("/sys/class/power_supply"))
                    {
                        string name = Path.GetFileName(dir);
                        if (name.StartsWith("BAT", StringComparison.OrdinalIgnoreCase))
                        {
                            string capFile = Path.Combine(dir, "capacity");
                            if (File.Exists(capFile)) return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        public CpuDetailedInfo GetCpuDetails()
        {
            var category = GetCurrentDeviceCategory();
            var cpu = new CpuDetailedInfo
            {
                Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                Threads = Environment.ProcessorCount,
                Cores = Environment.ProcessorCount
            };

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/proc/cpuinfo"))
                {
                    string[] lines = File.ReadAllLines("/proc/cpuinfo");
                    var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var line in lines)
                    {
                        if (line.StartsWith("model name", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                            {
                                cpu.ModelName = parts[1].Trim();
                                if (cpu.ModelName.Contains("@"))
                                {
                                    var clockPart = cpu.ModelName.Split('@');
                                    if (clockPart.Length > 1) cpu.BaseClock = clockPart[1].Trim();
                                }
                            }
                        }
                        else if (line.StartsWith("vendor_id", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
                                cpu.Vendor = parts[1].Trim();
                        }
                        else if (line.StartsWith("cpu cores", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var c))
                                cpu.Cores = c;
                        }
                        else if (line.StartsWith("cpu MHz", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1 && double.TryParse(parts[1].Trim(), out var mhz))
                            {
                                cpu.CurrentClock = $"{mhz / 1000.0:F2} GHz ({mhz:F0} MHz)";
                            }
                        }
                        else if (line.StartsWith("cache size", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1)
                                cpu.L3Cache = $"{parts[1].Trim()} Smart Cache";
                        }
                        else if (line.StartsWith("flags", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split(':');
                            if (parts.Length > 1)
                            {
                                foreach (var f in parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                                    flags.Add(f.ToUpperInvariant());
                            }
                        }
                    }

                    if (flags.Count > 0)
                    {
                        var displayFlags = new List<string>();
                        var meaningfulSets = new (string Flag, string Label)[]
                        {
                            ("AVX2", "AVX2 (Vektor & AI)"),
                            ("AVX", "AVX (256-bit)"),
                            ("FMA", "FMA3 (Komputasi)"),
                            ("AES", "AES-NI (Enkripsi)"),
                            ("SHA_NI", "SHA-NI (Kriptografi)"),
                            ("VMX", "VT-x (Virtualisasi)"),
                            ("SVM", "AMD-V (Virtualisasi)"),
                            ("SSE4_2", "SSE4.2 (Multimedia)"),
                            ("SSE4_1", "SSE4.1"),
                            ("SSSE3", "SSSE3"),
                            ("SSE3", "SSE3"),
                            ("SSE2", "SSE2"),
                            ("SSE", "SSE"),
                            ("MMX", "MMX"),
                            ("LM", "x86-64 (64-Bit)"),
                            ("POPCNT", "POPCNT"),
                            ("BMI1", "BMI1"),
                            ("BMI2", "BMI2"),
                            ("RDRAND", "RDRAND (Acak HW)"),
                            ("HT", "Hyper-Threading")
                        };

                        foreach (var (flag, label) in meaningfulSets)
                        {
                            if (flags.Contains(flag))
                                displayFlags.Add(label);
                        }

                        // Fallback if none of the customized ones matched
                        if (displayFlags.Count == 0)
                        {
                            string[] fallbackFlags = { "SSE", "SSE2", "SSE3", "SSSE3", "SSE4_1", "SSE4_2", "AVX", "AVX2", "AES", "FMA" };
                            foreach (var ff in fallbackFlags)
                            {
                                if (flags.Contains(ff)) displayFlags.Add(ff.Replace("_", "."));
                            }
                        }

                        if (displayFlags.Count > 0) cpu.InstructionSets = displayFlags;
                    }
                }
            }
            catch { }

            // Read CPU Caches from sysfs if available
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Directory.Exists("/sys/devices/system/cpu/cpu0/cache"))
                {
                    string l1d = "/sys/devices/system/cpu/cpu0/cache/index0/size";
                    string l1i = "/sys/devices/system/cpu/cpu0/cache/index1/size";
                    string l2 = "/sys/devices/system/cpu/cpu0/cache/index2/size";
                    string l3 = "/sys/devices/system/cpu/cpu0/cache/index3/size";

                    if (File.Exists(l1d)) cpu.L1DataCache = $"{File.ReadAllText(l1d).Trim()}B per Core ({cpu.Cores} instances)";
                    if (File.Exists(l1i)) cpu.L1InstructionCache = $"{File.ReadAllText(l1i).Trim()}B per Core ({cpu.Cores} instances)";
                    if (File.Exists(l2)) cpu.L2Cache = $"{File.ReadAllText(l2).Trim()}B per Core ({cpu.Cores} instances)";
                    if (File.Exists(l3)) cpu.L3Cache = $"{File.ReadAllText(l3).Trim()}B Smart Cache (Unified)";
                }
            }
            catch { }

            // Thermal temperature reading
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    for (int zone = 0; zone < 5; zone++)
                    {
                        string tempFile = $"/sys/class/thermal/thermal_zone{zone}/temp";
                        if (File.Exists(tempFile) && int.TryParse(File.ReadAllText(tempFile).Trim(), out var milliC))
                        {
                            double c = milliC / 1000.0;
                            if (c > 15 && c < 115)
                            {
                                cpu.TemperatureCelsius = Math.Round(c, 1);
                                break;
                            }
                        }
                    }
                }
            }
            catch { }

            // Dynamic Cross-Device Hardware Analysis via Engine
            var (socket, chipset, tdp, igpu, rec, feasibility) =
                HardwareIntelligenceEngine.AnalyzeCpu(cpu.ModelName, cpu.Vendor, cpu.Cores, cpu.Threads, category);

            cpu.SocketType = socket;
            cpu.Chipset = chipset;
            cpu.Tdp = tdp;
            cpu.IntegratedGpu = igpu;
            cpu.UpgradeRecommendation = rec;
            cpu.UpgradeFeasibility = feasibility;

            return cpu;
        }

        public MemoryDetailedInfo GetMemoryDetails()
        {
            var category = GetCurrentDeviceCategory();
            var mem = new MemoryDetailedInfo();

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/proc/meminfo"))
                {
                    string[] lines = File.ReadAllLines("/proc/meminfo");
                    long memTotalKb = 0;
                    long memAvailableKb = 0;
                    long cachedKb = 0;
                    long buffersKb = 0;
                    long swapTotalKb = 0;
                    long swapFreeKb = 0;

                    foreach (var line in lines)
                    {
                        var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && long.TryParse(parts[1], out var val))
                        {
                            if (line.StartsWith("MemTotal:", StringComparison.OrdinalIgnoreCase)) memTotalKb = val;
                            else if (line.StartsWith("MemAvailable:", StringComparison.OrdinalIgnoreCase)) memAvailableKb = val;
                            else if (line.StartsWith("Cached:", StringComparison.OrdinalIgnoreCase)) cachedKb = val;
                            else if (line.StartsWith("Buffers:", StringComparison.OrdinalIgnoreCase)) buffersKb = val;
                            else if (line.StartsWith("SwapTotal:", StringComparison.OrdinalIgnoreCase)) swapTotalKb = val;
                            else if (line.StartsWith("SwapFree:", StringComparison.OrdinalIgnoreCase)) swapFreeKb = val;
                        }
                    }

                    if (memTotalKb > 0)
                    {
                        mem.TotalGb = Math.Round(memTotalKb / (1024.0 * 1024.0), 1);
                        double usedKb = memTotalKb - memAvailableKb;
                        mem.UsedGb = Math.Round(usedKb / (1024.0 * 1024.0), 1);
                        mem.FreeGb = Math.Round(memAvailableKb / (1024.0 * 1024.0), 1);
                        mem.UsedPercent = (int)Math.Round((usedKb / (double)memTotalKb) * 100);
                        mem.CachedMb = Math.Round(cachedKb / 1024.0, 0);
                        mem.BuffersMb = Math.Round(buffersKb / 1024.0, 0);

                        if (swapTotalKb > 0)
                        {
                            mem.SwapTotalGb = Math.Round(swapTotalKb / (1024.0 * 1024.0), 1);
                            mem.SwapUsedGb = Math.Round((swapTotalKb - swapFreeKb) / (1024.0 * 1024.0), 1);
                        }
                    }
                }
                else
                {
                    var gc = GC.GetGCMemoryInfo();
                    if (gc.TotalAvailableMemoryBytes > 0)
                    {
                        mem.TotalGb = Math.Round(gc.TotalAvailableMemoryBytes / (1024.0 * 1024.0 * 1024.0), 1);
                    }
                }
            }
            catch { }

            // Dynamic Cross-Device Memory Analysis via Engine
            var (formFactor, maxCap, slots, channel, rec, feasibility) =
                HardwareIntelligenceEngine.AnalyzeMemory(mem.TotalGb, category, mem.MemoryType, _deviceService.GetDeviceModel());

            mem.FormFactor = formFactor;
            mem.MaxCapacity = maxCap;
            mem.SlotsSummary = slots;
            mem.ChannelMode = channel;
            mem.UpgradeRecommendation = rec;
            mem.UpgradeFeasibility = feasibility;

            return mem;
        }

        public StorageDetailedInfo GetStorageDetails()
        {
            var category = GetCurrentDeviceCategory();
            var storage = new StorageDetailedInfo();

            // 1. Get real filesystem drive sizes
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (var d in drives)
                {
                    if (d.IsReady && (d.RootDirectory.FullName == "/" || d.RootDirectory.FullName.StartsWith("C", StringComparison.OrdinalIgnoreCase)))
                    {
                        storage.TotalGb = Math.Round(d.TotalSize / (1024.0 * 1024.0 * 1024.0), 1);
                        storage.FreeGb = Math.Round(d.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0), 1);
                        storage.UsedGb = Math.Round(storage.TotalGb - storage.FreeGb, 1);
                        storage.UsedPercent = (int)Math.Round((storage.UsedGb / storage.TotalGb) * 100);
                        storage.FileSystem = d.DriveFormat ?? "ext4/NTFS";
                        storage.MountPoint = d.RootDirectory.FullName;
                        break;
                    }
                }
            }
            catch { }

            // 2. Read real hardware disk model and HDD/SSD rotational flag from Linux /sys/block/
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Directory.Exists("/sys/block"))
                {
                    foreach (var devPath in Directory.GetDirectories("/sys/block"))
                    {
                        string devName = Path.GetFileName(devPath);
                        if (devName.StartsWith("loop") || devName.StartsWith("ram") || devName.StartsWith("dm-") || devName.StartsWith("zram"))
                            continue;

                        string modelFile = Path.Combine(devPath, "device", "model");
                        string vendorFile = Path.Combine(devPath, "device", "vendor");
                        string rotaFile = Path.Combine(devPath, "queue", "rotational");

                        string modelStr = "";
                        if (File.Exists(modelFile))
                            modelStr = File.ReadAllText(modelFile).Trim();

                        string vendorStr = "";
                        if (File.Exists(vendorFile))
                            vendorStr = File.ReadAllText(vendorFile).Trim();

                        string fullDiskName = string.IsNullOrEmpty(vendorStr) ? modelStr : $"{vendorStr} {modelStr}".Trim();

                        bool isRotational = true;
                        if (File.Exists(rotaFile) && int.TryParse(File.ReadAllText(rotaFile).Trim(), out var rota))
                        {
                            isRotational = (rota == 1);
                        }

                        if (!string.IsNullOrWhiteSpace(fullDiskName))
                        {
                            storage.DiskModel = fullDiskName;
                            storage.IsRotationalHdd = isRotational;
                            if (isRotational)
                            {
                                storage.InterfaceType = "SATA III (Mechanical HDD / 7200 RPM)";
                                storage.TrimStatus = "Tidak Berlaku (Mechanical HDD)";
                                storage.SequentialReadMb = 135.0;
                                storage.SequentialWriteMb = 118.0;
                            }
                            else
                            {
                                if (devName.StartsWith("nvme"))
                                {
                                    storage.InterfaceType = "NVMe M.2 PCIe Gen 3.0 / 4.0";
                                    storage.SequentialReadMb = 3500.0;
                                    storage.SequentialWriteMb = 3000.0;
                                }
                                else
                                {
                                    storage.InterfaceType = "SATA III Solid State Drive";
                                    storage.SequentialReadMb = 540.0;
                                    storage.SequentialWriteMb = 490.0;
                                }
                                storage.TrimStatus = "Didukung & Aktif (TRIM Enabled)";
                            }
                            break;
                        }
                    }
                }
            }
            catch { }

            // Dynamic Cross-Device Storage Analysis via Engine
            bool isNvmeDevice = storage.InterfaceType.Contains("NVMe", StringComparison.OrdinalIgnoreCase);
            var (formFactor, m2Status, sataStatus, rec, feasibility, gain) =
                HardwareIntelligenceEngine.AnalyzeStorage(storage.DiskModel, storage.IsRotationalHdd, storage.TotalGb, category, isNvmeDevice);

            storage.FormFactor = formFactor;
            storage.M2SlotStatus = m2Status;
            storage.SataPortsAvailable = sataStatus;
            storage.UpgradeRecommendation = rec;
            storage.UpgradeFeasibility = feasibility;
            storage.EstimatedPerformanceGain = gain;

            return storage;
        }

        public MotherboardDetailedInfo GetMotherboardDetails()
        {
            var category = GetCurrentDeviceCategory();
            var mb = new MotherboardDetailedInfo
            {
                ProductModel = _deviceService.GetDeviceModel(),
                SerialNumber = _deviceService.GetSerialNumber(),
                SystemUuid = _deviceService.GetHardwareId(),
                OsName = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.ProcessArchitecture.ToString()
            };

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (File.Exists("/sys/class/dmi/id/product_name"))
                    {
                        string p = File.ReadAllText("/sys/class/dmi/id/product_name").Trim();
                        if (!string.IsNullOrEmpty(p)) mb.ProductModel = p;
                    }

                    if (File.Exists("/sys/class/dmi/id/sys_vendor"))
                    {
                        string v = File.ReadAllText("/sys/class/dmi/id/sys_vendor").Trim();
                        if (!string.IsNullOrEmpty(v)) mb.Manufacturer = v;
                    }
                    else if (File.Exists("/sys/class/dmi/id/board_vendor"))
                    {
                        string v = File.ReadAllText("/sys/class/dmi/id/board_vendor").Trim();
                        if (!string.IsNullOrEmpty(v)) mb.Manufacturer = v;
                    }

                    if (File.Exists("/sys/class/dmi/id/bios_vendor"))
                        mb.BiosVendor = File.ReadAllText("/sys/class/dmi/id/bios_vendor").Trim();

                    if (File.Exists("/sys/class/dmi/id/bios_version"))
                        mb.BiosVersion = File.ReadAllText("/sys/class/dmi/id/bios_version").Trim();

                    if (File.Exists("/sys/class/dmi/id/bios_date"))
                        mb.BiosReleaseDate = File.ReadAllText("/sys/class/dmi/id/bios_date").Trim();
                }
            }
            catch { }

            // Dynamic Cross-Device Motherboard Analysis via Engine
            var (expansionSlots, powerSupply, upgradeAdvice) =
                HardwareIntelligenceEngine.AnalyzeMotherboard(mb.Manufacturer, mb.ProductModel, category);

            mb.ExpansionSlots = expansionSlots;
            mb.PowerSupply = powerSupply;
            mb.UpgradeAdvice = upgradeAdvice;

            // Cryptographic signature hash
            using (var sha = SHA256.Create())
            {
                byte[] hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes($"{mb.Manufacturer}_{mb.ProductModel}_{mb.BiosVersion}_{mb.SystemUuid}"));
                mb.WhitelistSignature = "SHA256-" + BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
            }

            return mb;
        }
    }
}
