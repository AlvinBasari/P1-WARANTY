using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SharedCore.Models;
using SharedCore.Services;
using TechnicianApp.ViewModels;
using Xunit;

namespace DesktopApp.Tests
{
    public class TechnicianAppTests
    {
        [Fact]
        public void TechnicianAuthViewModel_InitialState_HasDefaultCredentials()
        {
            var apiClient = new ApiClient();
            var vm = new TechnicianAuthViewModel(apiClient, _ => { });

            Assert.Equal("technician@warranty.com", vm.Email);
            Assert.Equal("password", vm.Password);
            Assert.False(vm.IsLoading);
            Assert.Null(vm.ErrorMessage);
        }

        [Fact]
        public void TechnicianAuthViewModel_FillDemoCredentials_SetsExpectedValues()
        {
            var apiClient = new ApiClient();
            var vm = new TechnicianAuthViewModel(apiClient, _ => { })
            {
                Email = "custom@example.com",
                Password = "custompassword",
                ErrorMessage = "Some error"
            };

            vm.FillDemoCredentials();

            Assert.Equal("technician@warranty.com", vm.Email);
            Assert.Equal("password", vm.Password);
            Assert.Null(vm.ErrorMessage);
        }

        [Fact]
        public void TicketQueueViewModel_Filtering_FiltersByCategoryAndStatus()
        {
            var apiClient = new ApiClient();
            var vm = new TicketQueueViewModel(
                apiClient,
                _ => { },
                _ => { },
                _ => { },
                _ => { }
            );

            // Populate sample tickets
            var sampleTickets = new List<RepairRequestDto>
            {
                new() { Id = 1, Type = "remote", DamageCategory = "non_physical", Status = "pending", Description = "Layar freeze saat boot", User = new UserDto { Name = "Andi" }, Device = new DeviceDto { Model = "ROG Strix G15", SerialNumber = "ROG-001" } },
                new() { Id = 2, Type = "on_site", DamageCategory = "physical", Status = "scheduled", Description = "Keyboard rusak tersiram air", NeedsOfficeRepair = false, User = new UserDto { Name = "Budi" }, Device = new DeviceDto { Model = "ZenBook 14", SerialNumber = "ZEN-002" } },
                new() { Id = 3, Type = "on_site", DamageCategory = "physical", Status = "in_progress", Description = "Motherboard mati total", NeedsOfficeRepair = true, Tracking = new RepairTrackingDto { CurrentStatus = "sedang_diperbaiki" }, User = new UserDto { Name = "Citra" }, Device = new DeviceDto { Model = "TUF Gaming A15", SerialNumber = "TUF-003" } },
                new() { Id = 4, Type = "remote", DamageCategory = "non_physical", Status = "completed", Description = "Driver audio glitch", User = new UserDto { Name = "Dewi" }, Device = new DeviceDto { Model = "VivoBook S14", SerialNumber = "VIV-004" } },
            };

            // Use reflection or set internal list to test filtering
            var allField = typeof(TicketQueueViewModel).GetField("_allTickets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            allField?.SetValue(vm, sampleTickets);

            // 1. All tab
            vm.SelectTab("all");
            Assert.Equal(4, vm.FilteredTickets.Count);

            // 2. Remote tab
            vm.SelectTab("remote");
            Assert.Equal(2, vm.FilteredTickets.Count);
            Assert.All(vm.FilteredTickets, t => Assert.True(t.Type == "remote" || t.DamageCategory == "non_physical"));

            // 3. On-Site tab
            vm.SelectTab("onsite");
            Assert.Single(vm.FilteredTickets);
            Assert.Equal(2, vm.FilteredTickets.First().Id);

            // 4. Workshop tab
            vm.SelectTab("workshop");
            Assert.Single(vm.FilteredTickets);
            Assert.Equal(3, vm.FilteredTickets.First().Id);

            // 5. Completed tab
            vm.SelectTab("completed");
            Assert.Single(vm.FilteredTickets);
            Assert.Equal(4, vm.FilteredTickets.First().Id);

            // 6. Keyword search
            vm.SelectTab("all");
            vm.SearchKeyword = "ROG";
            Assert.Single(vm.FilteredTickets);
            Assert.Equal(1, vm.FilteredTickets.First().Id);
        }

        [Fact]
        public void TicketDetailViewModel_Properties_ExposesCustomerAndDeviceData()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var ticket = new RepairRequestDto
            {
                Id = 10,
                Type = "remote",
                DamageCategory = "non_physical",
                Description = "Thermal throttling saat rendering",
                LocationLat = -7.7956,
                LocationLng = 110.3695,
                User = new UserDto
                {
                    Name = "Alvin Pratama",
                    Email = "alvin@gmail.com",
                    Phone = "081234567890",
                    WhatsappNumber = "081234567890",
                    Address = "Jl. Malioboro No. 12, Yogyakarta"
                },
                Device = new DeviceDto
                {
                    Model = "ASUS ROG Zephyrus G14",
                    SerialNumber = "SN-ROG-2026",
                    LocationLabel = "Kantor Cabang Jogja",
                    Warranty = new WarrantyDto { Status = "active", WarrantyEnd = "2027-09-01" }
                },
                RemoteSession = new RemoteSessionDto
                {
                    Id = 5,
                    RustdeskSessionId = "948123456",
                    ConnectionStatus = "scheduled"
                }
            };

            var vm = new TicketDetailViewModel(
                apiClient,
                rustDesk,
                ticket,
                () => { },
                _ => { },
                _ => { }
            );

            Assert.True(vm.IsRemoteTicket);
            Assert.False(vm.IsOnSiteTicket);
            Assert.Equal("948123456", vm.RustdeskSessionId);
            Assert.Equal("Alvin Pratama", vm.CustomerName);
            Assert.Equal("alvin@gmail.com", vm.CustomerEmail);
            Assert.Equal("ASUS ROG Zephyrus G14", vm.DeviceModel);
            Assert.Equal("SN-ROG-2026", vm.DeviceSerial);
            Assert.True(vm.HasLocation);
            Assert.Contains("-7.79560", vm.LocationCoordinatesText);
        }

        [Fact]
        public void WorkshopTrackingViewModel_StatusIndexing_CalculatesStepProgression()
        {
            var apiClient = new ApiClient();
            var ticket = new RepairRequestDto
            {
                Id = 20,
                Tracking = new RepairTrackingDto
                {
                    CurrentStatus = "sedang_diperbaiki",
                    Histories = new List<RepairTrackingHistoryDto>
                    {
                        new() { Status = "dijemput", Notes = "Unit diterima kurir" },
                        new() { Status = "di_service_center", Notes = "Tiba di Service Center" },
                        new() { Status = "sedang_diperbaiki", Notes = "Sedang perbaikan motherboard" }
                    }
                }
            };

            var vm = new WorkshopTrackingViewModel(apiClient, ticket, () => { });

            Assert.Equal("sedang_diperbaiki", vm.CurrentStatus);
            Assert.True(vm.IsStep1Completed);
            Assert.True(vm.IsStep2Completed);
            Assert.True(vm.IsStep3Completed);
            Assert.False(vm.IsStep4Completed);
            Assert.False(vm.IsStep5Completed);
            Assert.True(vm.IsStep3Active);
            Assert.Equal(3, vm.Histories.Count);
        }

        [Fact]
        public void InvoiceManagerViewModel_Calculations_ComputesWarrantyDiscountsAndPayables()
        {
            var apiClient = new ApiClient();
            var ticket = new RepairRequestDto { Id = 30 };
            var vm = new InvoiceManagerViewModel(apiClient, ticket, () => { });

            // Clear defaults
            vm.Items.Clear();

            // 1. Add covered item (e.g. Sparepart Baterai Rp 500.000 covered)
            vm.NewItemName = "Baterai Li-Ion 65Wh Original";
            vm.NewItemQuantity = 1;
            vm.NewItemUnitPrice = 500000;
            vm.NewItemIsCovered = true;
            vm.AddItem();

            Assert.Single(vm.Items);
            Assert.Equal(500000, vm.SubtotalAmount);
            Assert.Equal(500000, vm.WarrantyDiscountAmount);
            Assert.Equal(0, vm.TotalPayableAmount);
            Assert.Equal("paid_by_warranty", vm.PaymentStatus);

            // 2. Add non-covered item (e.g. Custom Cleaning Paste Rp 150.000 non-covered)
            vm.NewItemName = "Thermal Paste Liquid Metal Custom";
            vm.NewItemQuantity = 1;
            vm.NewItemUnitPrice = 150000;
            vm.NewItemIsCovered = false;
            vm.AddItem();

            Assert.Equal(2, vm.Items.Count);
            Assert.Equal(650000, vm.SubtotalAmount);
            Assert.Equal(500000, vm.WarrantyDiscountAmount);
            Assert.Equal(150000, vm.TotalPayableAmount);
            Assert.Equal("unpaid", vm.PaymentStatus);

            // 3. Remove non-covered item
            vm.RemoveItem(vm.Items[1]);

            Assert.Single(vm.Items);
            Assert.Equal(500000, vm.SubtotalAmount);
            Assert.Equal(500000, vm.WarrantyDiscountAmount);
            Assert.Equal(0, vm.TotalPayableAmount);
            Assert.Equal("paid_by_warranty", vm.PaymentStatus);
        }

        [Fact]
        public void TechnicianMainViewModel_NavigationAndTheme()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var themeService = new ThemeSettingsService();

            var vm = new TechnicianMainViewModel(apiClient, rustDesk, themeService);

            // Initial State -> Auth Page
            Assert.True(vm.IsAuthPage);
            Assert.False(vm.IsAuthenticated);
            Assert.False(vm.IsQueuePage);
            Assert.False(vm.IsDetailPage);

            // Login Success -> Queue Page
            var techUser = new UserDto { Id = 2, Name = "Budi Santoso", Role = "technician", Email = "technician@warranty.com" };
            vm.OnLoginSuccess(techUser);

            Assert.True(vm.IsAuthenticated);
            Assert.True(vm.IsQueuePage);
            Assert.False(vm.IsAuthPage);
            Assert.Equal("Budi Santoso", vm.TechnicianName);

            // Logout -> Triggers Confirmation Dialog -> Confirm -> Back to Auth Page
            vm.Logout();
            Assert.True(vm.IsConfirmDialogOpen);
            Assert.NotNull(vm.ConfirmDialogViewModel);
            Assert.Equal("danger", vm.ConfirmDialogViewModel.DialogType);
            vm.ConfirmDialogViewModel.Confirm();

            Assert.False(vm.IsAuthenticated);
            Assert.True(vm.IsAuthPage);
            Assert.False(vm.IsConfirmDialogOpen);
        }
    }
}
