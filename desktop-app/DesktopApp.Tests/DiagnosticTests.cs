using System.IO;
using System.Threading.Tasks;
using CustomerApp.Services;
using CustomerApp.ViewModels;
using SharedCore.Services;
using Xunit;

namespace DesktopApp.Tests
{
    public class DiagnosticTests
    {
        [Fact]
        public void DiagnosticViewModel_InitialState_InitializesCorrectly()
        {
            var apiClient = new ApiClient();
            var deviceService = new DeviceIdentifierService();
            var vm = new DiagnosticViewModel(apiClient, deviceService);

            Assert.Equal(100, vm.OverallHealthScore);
            Assert.False(vm.IsScanning);
            Assert.False(string.IsNullOrWhiteSpace(vm.HardwareModel));
            Assert.False(string.IsNullOrWhiteSpace(vm.SerialNumber));
            Assert.NotNull(vm.CpuTest);
            Assert.NotNull(vm.RamTest);
            Assert.NotNull(vm.StorageTest);
            Assert.NotNull(vm.PowerTest);
            Assert.NotNull(vm.BiosTest);
            Assert.NotNull(vm.NetworkTest);
            Assert.True(vm.CpuTest.IsPassed);
            Assert.True(vm.RamTest.IsPassed);
            Assert.True(vm.StorageTest.IsPassed);
        }

        [Fact]
        public async Task DiagnosticViewModel_TestSingleComponent_CompletesSuccessfully()
        {
            var apiClient = new ApiClient();
            var deviceService = new DeviceIdentifierService();
            var vm = new DiagnosticViewModel(apiClient, deviceService);

            await vm.TestSingleComponentAsync("CPU");
            Assert.True(vm.CpuTest.IsPassed);
            Assert.Contains("Core", vm.CpuTest.Detail);

            await vm.TestSingleComponentAsync("RAM");
            Assert.True(vm.RamTest.IsPassed);
            Assert.Contains("MB", vm.RamTest.Detail);

            await vm.TestSingleComponentAsync("Storage");
            Assert.True(vm.StorageTest.IsPassed);
            Assert.Contains("MB/s", vm.StorageTest.Detail);
        }

        [Fact]
        public async Task DiagnosticViewModel_RunFullScan_ExecutesAllModules()
        {
            var apiClient = new ApiClient();
            var deviceService = new DeviceIdentifierService();
            var vm = new DiagnosticViewModel(apiClient, deviceService);

            await vm.RunFullScanAsync();

            Assert.False(vm.IsScanning);
            Assert.Equal(100, vm.OverallHealthScore);
            Assert.Equal(100, vm.ScanProgress);
            Assert.True(vm.CpuTest.IsPassed);
            Assert.True(vm.RamTest.IsPassed);
            Assert.True(vm.StorageTest.IsPassed);
            Assert.True(vm.PowerTest.IsPassed);
            Assert.True(vm.BiosTest.IsPassed);
            Assert.True(vm.NetworkTest.IsPassed);
        }

        [Fact]
        public void DiagnosticViewModel_ExportReport_GeneratesValidTxtFile()
        {
            var apiClient = new ApiClient();
            var deviceService = new DeviceIdentifierService();
            var vm = new DiagnosticViewModel(apiClient, deviceService);

            vm.ExportReport();

            string downloadsDir = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
                "Downloads"
            );
            string safeSerial = vm.SerialNumber.Replace('/', '_').Replace('\\', '_').Replace(':', '_');
            string expectedPath = Path.Combine(downloadsDir, $"Laporan_Diagnostik_JTS_{safeSerial}.txt");

            Assert.True(File.Exists(expectedPath));
            string content = File.ReadAllText(expectedPath);
            Assert.Contains("LAPORAN RESMI DIAGNOSTIK HARDWARE", content);
            Assert.Contains("1. Processor / CPU Stress", content);
            Assert.Contains("2. Memori RAM & Cache", content);
            Assert.Contains("3. Storage (Penyimpanan)", content);

            // Cleanup test artifact
            try { File.Delete(expectedPath); } catch { }
        }

        [Fact]
        public async Task DiagnosticViewModel_AuditLogs_TracksEventsAndCanBeCopied()
        {
            var apiClient = new ApiClient();
            var deviceService = new DeviceIdentifierService();
            var vm = new DiagnosticViewModel(apiClient, deviceService);

            Assert.NotEmpty(vm.DiagnosticLogs);
            int initialCount = vm.DiagnosticLogs.Count;

            await vm.TestSingleComponentAsync("CPU");
            Assert.True(vm.DiagnosticLogs.Count > initialCount);

            // Test copy logs command
            await vm.CopyLogsAsync();
            Assert.NotNull(vm.ToastMessage);
            Assert.Contains("disalin", vm.ToastMessage, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task HardwareDiagnosticService_RealCpuBenchmark_ExecutesWithoutError()
        {
            var service = new HardwareDiagnosticService();
            var res = await service.RunCpuStressTestAsync(300);

            Assert.True(res.IsPassed);
            Assert.True(res.CoresUtilized >= 1);
            Assert.True(res.GigaOpsPerSec >= 0);
            Assert.False(string.IsNullOrWhiteSpace(res.Detail));
        }

        [Fact]
        public async Task HardwareDiagnosticService_RealRamIntegrityTest_VerifiesBitPatterns()
        {
            var service = new HardwareDiagnosticService();
            var res = await service.RunRamIntegrityTestAsync(16);

            Assert.True(res.IsPassed);
            Assert.Equal(0, res.ErrorCount);
            Assert.True(res.BandwidthGbPerSec > 0);
        }

        [Fact]
        public async Task HardwareDiagnosticService_RealStorageIoBenchmark_MeasuresReadWriteSpeeds()
        {
            var service = new HardwareDiagnosticService();
            var res = await service.RunStorageIoBenchmarkAsync(8);

            Assert.True(res.IsPassed);
            Assert.True(res.WriteMbPerSec > 0);
            Assert.True(res.ReadMbPerSec > 0);
        }

        [Fact]
        public async Task HardwareDiagnosticService_PowerAndThermalCheck_ReturnsValidData()
        {
            var service = new HardwareDiagnosticService();
            var res = await service.RunPowerAndThermalCheckAsync();

            Assert.True(res.IsPassed);
            Assert.False(string.IsNullOrWhiteSpace(res.PowerSource));
            Assert.False(string.IsNullOrWhiteSpace(res.Detail));
        }

        [Fact]
        public async Task HardwareDiagnosticService_BiosIntegrityCheck_GeneratesSignature()
        {
            var service = new HardwareDiagnosticService();
            var res = await service.RunBiosIntegrityCheckAsync();

            Assert.True(res.IsPassed);
            Assert.False(string.IsNullOrWhiteSpace(res.BiosVersion));
            Assert.False(string.IsNullOrWhiteSpace(res.HardwareSignatureHash));
        }

        [Fact]
        public async Task HardwareDiagnosticService_NetworkPingAndJitterTest_ReturnsMeasurements()
        {
            var service = new HardwareDiagnosticService();
            var res = await service.RunNetworkPingAndJitterTestAsync();

            Assert.True(res.IsPassed);
            Assert.True(res.AvgLatencyMs >= 0);
            Assert.True(res.SuccessRatePercent > 0);
        }

        [Fact]
        public void DetailedHardwareInfoService_ExtractsDeepCpuAndMemorySpecs()
        {
            var service = new DetailedHardwareInfoService();
            var cpu = service.GetCpuDetails();
            var mem = service.GetMemoryDetails();
            var storage = service.GetStorageDetails();
            var mb = service.GetMotherboardDetails();

            Assert.False(string.IsNullOrWhiteSpace(cpu.ModelName));
            Assert.True(cpu.Cores >= 1);
            Assert.True(cpu.InstructionSets.Count > 0);

            Assert.True(mem.TotalGb > 0);
            Assert.False(string.IsNullOrWhiteSpace(mem.MemoryType));

            Assert.True(storage.TotalGb > 0);
            Assert.False(string.IsNullOrWhiteSpace(storage.FileSystem));

            Assert.False(string.IsNullOrWhiteSpace(mb.ProductModel));
            Assert.False(string.IsNullOrWhiteSpace(mb.WhitelistSignature));
        }

        [Fact]
        public void HardwareIntelligenceEngine_AccuratelyAnalyzesVariousDevicesAndArchitectures()
        {
            // 1. Test Laptop (Asus Vivobook / ThinkPad with mobile CPU)
            var laptopCat = HardwareIntelligenceEngine.DetectDeviceCategory("ASUSTeK", "Vivobook 14 X415EA", hasBattery: true);
            Assert.Equal(DeviceCategory.Laptop, laptopCat);

            var laptopCpu = HardwareIntelligenceEngine.AnalyzeCpu("11th Gen Intel(R) Core(TM) i5-1135G7 @ 2.40GHz", "GenuineIntel", 4, 8, laptopCat);
            Assert.Contains("BGA", laptopCpu.Socket);
            Assert.Contains("Tersolder", laptopCpu.Feasibility);

            var laptopMem = HardwareIntelligenceEngine.AnalyzeMemory(8.0, laptopCat);
            Assert.Contains("SO-DIMM", laptopMem.FormFactor);
            Assert.Contains("Dual-Channel", laptopMem.Recommendation);

            var laptopStorage = HardwareIntelligenceEngine.AnalyzeStorage("SAMSUNG MZVL2512HCJQ-00B00 (512 GB)", isRotationalHdd: false, totalGb: 512.0, laptopCat);
            Assert.Contains("NVMe", laptopStorage.FormFactor);

            var laptopMb = HardwareIntelligenceEngine.AnalyzeMotherboard("ASUSTeK", "X415EA", laptopCat);
            Assert.Contains("Baterai", laptopMb.PowerSupply);

            // 2. Test Desktop SFF (Lenovo ThinkCentre with Core i3-7100 and HDD)
            var sffCat = HardwareIntelligenceEngine.DetectDeviceCategory("LENOVO", "10MAS0FB00 ThinkCentre M710s", hasBattery: false);
            Assert.Equal(DeviceCategory.DesktopSff, sffCat);

            var sffCpu = HardwareIntelligenceEngine.AnalyzeCpu("Intel(R) Core(TM) i3-7100 CPU @ 3.90GHz", "GenuineIntel", 2, 4, sffCat);
            Assert.Contains("LGA1151", sffCpu.Socket);
            Assert.Contains("Di-upgrade", sffCpu.Feasibility);

            var sffStorage = HardwareIntelligenceEngine.AnalyzeStorage("HGST HTS725050A7E630", isRotationalHdd: true, totalGb: 500.0, sffCat);
            Assert.Contains("disarankan", sffStorage.Recommendation, StringComparison.OrdinalIgnoreCase);

            var sffMem = HardwareIntelligenceEngine.AnalyzeMemory(8.0, sffCat, "DDR4 SDRAM", "10MAS0FB00 ThinkCentre M710s");
            Assert.Contains("3 Slot Kosong", sffMem.SlotsSummary);
            Assert.Contains("4 Slot DIMM", sffMem.MaxCapacity);

            // 3. Test Desktop Tower (AMD Ryzen 5 5600 Gaming PC)
            var desktopCat = HardwareIntelligenceEngine.DetectDeviceCategory("Micro-Star International Co., Ltd.", "MS-7C95 B550 GAMING PLUS", hasBattery: false);
            Assert.Equal(DeviceCategory.DesktopTower, desktopCat);

            var amdCpu = HardwareIntelligenceEngine.AnalyzeCpu("AMD Ryzen 5 5600 6-Core Processor", "AuthenticAMD", 6, 12, desktopCat);
            Assert.Contains("Socket AM4", amdCpu.Socket);
            Assert.Contains("Ryzen 7 5700X", amdCpu.Recommendation);

            var desktopMem = HardwareIntelligenceEngine.AnalyzeMemory(16.0, desktopCat);
            Assert.Contains("UDIMM", desktopMem.FormFactor);
            Assert.Contains("16 GB", desktopMem.Recommendation);

            // 4. Test Modern Intel Desktop (Core i5-13400 / i7-14700 LGA1700)
            var modernCpu = HardwareIntelligenceEngine.AnalyzeCpu("13th Gen Intel(R) Core(TM) i5-13400", "GenuineIntel", 10, 16, desktopCat);
            Assert.Contains("LGA1700", modernCpu.Socket);
        }

        [Fact]
        public async Task ComponentDetailViewModel_CategorySelectionAndBenchmarkExecution()
        {
            string navigatedTab = string.Empty;
            var vm = new ComponentDetailViewModel(tab => navigatedTab = tab);

            Assert.Equal("CPU", vm.SelectedCategory);
            Assert.NotNull(vm.CpuInfo);

            vm.SelectCategory("RAM");
            Assert.Equal("RAM", vm.SelectedCategory);

            vm.SelectCategory("Storage");
            Assert.Equal("Storage", vm.SelectedCategory);

            await vm.RunActiveBenchmarkAsync();
            Assert.False(vm.IsBenchmarking);
            Assert.False(string.IsNullOrWhiteSpace(vm.BenchmarkResultText));

            vm.BackToDashboard();
            Assert.Equal("Warranty", navigatedTab);
        }

        [Fact]
        public void GenerateWarrantyCardImage_CleanLightWithQr_GeneratesValidPng()
        {
            string savedPath = WarrantyCardImageGenerator.GenerateAndSaveCardImage(
                "Lenovo ThinkPad X1 Carbon Gen 10",
                "JTS-TEST-QR-9900",
                "HW-UUID-TEST-CLEAN-LIGHT",
                "Alvin (Corporate Customer)",
                "36 Bulan Resmi PT JTS",
                "2027-12-31",
                "GARANSI RESMI AKTIF",
                "VALID-TOKEN-QR-XYZ-123"
            );

            Assert.True(File.Exists(savedPath));
            var fileInfo = new FileInfo(savedPath);
            Assert.True(fileInfo.Length > 2000); // Has bitmap + rendered QR code

            // Cleanup
            try { File.Delete(savedPath); } catch { }
        }
    }
}
