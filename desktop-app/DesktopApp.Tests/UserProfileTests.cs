using System;
using CustomerApp.ViewModels;
using SharedCore.Models;
using SharedCore.Services;
using Xunit;

namespace DesktopApp.Tests
{
    public class UserProfileTests
    {
        [Fact]
        public void LocationViewModel_InitializesWithUserDto_CorrectlySetsFields()
        {
            var apiClient = new ApiClient("http://localhost:8000");
            var user = new UserDto
            {
                Id = 5,
                Name = "Alvin CS",
                Email = "alvin@example.com",
                Phone = "08123456789",
                WhatsappNumber = "081234567890",
                AvatarUrl = "http://localhost:8000/storage/avatars/avatar_5.jpg"
            };

            var device = new DeviceDto
            {
                Id = 1,
                SerialNumber = "SN123456",
                LocationLat = -6.225000,
                LocationLng = 106.800000,
                LocationLabel = "Kantor - Jl. Sudirman No. 10 (Dekat Halte)"
            };

            var vm = new LocationViewModel(
                apiClient,
                () => device,
                d => { },
                () => user,
                u => { }
            );

            Assert.Equal("Alvin CS", vm.UserName);
            Assert.Equal("alvin@example.com", vm.UserEmail);
            Assert.Equal("081234567890", vm.UserWhatsapp);
            Assert.Equal("http://localhost:8000/storage/avatars/avatar_5.jpg", vm.UserAvatarUrl);
            Assert.Equal("A", vm.UserInitial);
            Assert.Equal("Kantor", vm.LocationType);
            Assert.Equal("Jl. Sudirman No. 10 (Dekat Halte)", vm.Street);
            Assert.Equal(-6.225000, vm.Latitude);
            Assert.Equal(106.800000, vm.Longitude);
        }

        [Fact]
        public void LocationViewModel_UpdatesInitial_WhenUserNameChanges()
        {
            var apiClient = new ApiClient("http://localhost:8000");
            var vm = new LocationViewModel(apiClient, () => null, _ => { });

            vm.UserName = "Budi Santoso";
            Assert.Equal("B", vm.UserInitial);

            vm.UserName = "   ";
            Assert.Equal("U", vm.UserInitial);
        }
    }
}
