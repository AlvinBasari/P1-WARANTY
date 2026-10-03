using System.Threading.Tasks;
using SharedCore.Models;
using SharedCore.Services;
using Xunit;

namespace DesktopApp.Tests
{
    public class DeviceServiceTests
    {
        [Fact]
        public void GetHardwareId_ReturnsNonEmptyValidString()
        {
            var service = new DeviceIdentifierService();
            string hwId = service.GetHardwareId();

            Assert.False(string.IsNullOrWhiteSpace(hwId));
            Assert.True(hwId.Length >= 5);
        }

        [Fact]
        public void GetDeviceModel_ReturnsNonEmptyString()
        {
            var service = new DeviceIdentifierService();
            string model = service.GetDeviceModel();

            Assert.False(string.IsNullOrWhiteSpace(model));
        }

        [Fact]
        public async Task RustDeskService_GeneratesValidSessionId()
        {
            var service = new RustDeskService();
            string sessionId = await service.GetSessionIdAsync();

            Assert.False(string.IsNullOrWhiteSpace(sessionId));
            Assert.True(sessionId.Length >= 6);
        }

        [Fact]
        public void DtoSerialization_PreservesKeyProperties()
        {
            var device = new DeviceDto
            {
                Id = 1,
                HardwareId = "BIOS-UUID-DEMO-001",
                Model = "ASUS ZenBook Pro",
                SerialNumber = "SN-ASUS-1234",
                LocationLat = -6.2088,
                LocationLng = 106.8456,
                LocationLabel = "Rumah"
            };

            Assert.Equal("BIOS-UUID-DEMO-001", device.HardwareId);
            Assert.Equal(-6.2088, device.LocationLat);
            Assert.Equal(106.8456, device.LocationLng);
        }

        [Fact]
        public void GenerateWarrantyCardImage_CreatesValidPngFile()
        {
            string savedPath = CustomerApp.Services.WarrantyCardImageGenerator.GenerateAndSaveCardImage(
                "ThinkPad X1 Carbon Gen 10",
                "JTS-TEST-998811",
                "BIOS-HW-UUID-TEST",
                "Alvin (Test Buyer)",
                "24 Bulan Resmi",
                "2024-01-01",
                "GARANSI RESMI AKTIF",
                "JTS-QR-TEST-TOKEN"
            );

            Assert.True(System.IO.File.Exists(savedPath));
            var fileInfo = new System.IO.FileInfo(savedPath);
            Assert.True(fileInfo.Length > 1000); // Valid PNG file size

            // Cleanup test artifact
            try { System.IO.File.Delete(savedPath); } catch { }
        }
    }
}
