using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CustomerApp.ViewModels;
using SharedCore.Models;
using SharedCore.Services;
using TechnicianApp.ViewModels;
using Xunit;

namespace DesktopApp.Tests
{
    public class ConfirmationModalTests
    {
        [Fact]
        public void CustomerApp_Logout_ShowsDangerConfirmModal_And_CancellingRetainsLogin()
        {
            var apiClient = new ApiClient();
            var deviceService = new DeviceIdentifierService();
            var rustDesk = new RustDeskService();
            var vm = new MainViewModel(apiClient, deviceService, rustDesk);

            // Simulate logged in user
            var user = new UserDto { Id = 1, Name = "Alvin", Email = "alvin@example.com" };
            vm.AuthVm.Email = "alvin@example.com";
            vm.CurrentUser = user;
            vm.IsAuthenticated = true;

            // Trigger Logout
            vm.Logout();

            Assert.True(vm.IsConfirmDialogOpen);
            Assert.NotNull(vm.ConfirmDialogViewModel);
            Assert.Equal("danger", vm.ConfirmDialogViewModel.DialogType);
            Assert.Contains("Konfirmasi Keluar Akun", vm.ConfirmDialogViewModel.Title);

            // Cancel logout -> should remain authenticated
            vm.ConfirmDialogViewModel.Cancel();
            Assert.False(vm.IsConfirmDialogOpen);
            Assert.True(vm.IsAuthenticated);
            Assert.NotNull(vm.CurrentUser);

            // Trigger Logout again -> Confirm -> logs out
            vm.Logout();
            Assert.True(vm.IsConfirmDialogOpen);
            vm.ConfirmDialogViewModel.Confirm();

            Assert.False(vm.IsConfirmDialogOpen);
            Assert.False(vm.IsAuthenticated);
            Assert.Null(vm.CurrentUser);
        }

        [Fact]
        public async Task CustomerApp_ClaimSubmit_ShowsConfirmationModal_WithActiveWarranty()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var device = new DeviceDto
            {
                Id = 10,
                Model = "ASUS ROG Zephyrus G14",
                SerialNumber = "SN-ROG-2026",
                Warranty = new WarrantyDto { Status = "active", WarrantyEnd = "2027-12-31" }
            };

            CustomerApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new ClaimViewModel(
                apiClient,
                rustDesk,
                () => device,
                _ => { },
                dialog => capturedDialog = dialog
            );

            vm.Description = "Layar sering berkedip dan muncul garis artefak saat beban GPU tinggi";
            vm.IsPhysicalDamage = false;

            await vm.SubmitClaimAsync();

            Assert.NotNull(capturedDialog);
            Assert.Contains("Konfirmasi Pengajuan Servis", capturedDialog.Title);
            Assert.Equal("BANTUAN REMOTE", capturedDialog.BadgeText);
            Assert.Equal("info", capturedDialog.DialogType);
            Assert.Contains("ASUS ROG", capturedDialog.DetailNote);
            Assert.Contains("AKTIF", capturedDialog.DetailNote);
            Assert.False(vm.IsDeviceWarrantyExpired);
            Assert.True(vm.IsDeviceWarrantyActive);
        }

        [Fact]
        public async Task CustomerApp_ClaimSubmit_ShowsWarningModal_WhenWarrantyExpired()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var device = new DeviceDto
            {
                Id = 11,
                Model = "ThinkPad X1 Carbon",
                SerialNumber = "SN-THINK-2024",
                Warranty = new WarrantyDto { Status = "expired", WarrantyEnd = "2025-01-01" }
            };

            CustomerApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new ClaimViewModel(
                apiClient,
                rustDesk,
                () => device,
                _ => { },
                dialog => capturedDialog = dialog
            );

            vm.Description = "Baterai kembung dan touchpad terangkat tidak bisa diklik";
            vm.IsPhysicalDamage = true;

            await vm.SubmitClaimAsync();

            Assert.NotNull(capturedDialog);
            Assert.Contains("Konfirmasi Pengajuan Servis", capturedDialog.Title);
            Assert.Equal("SERVIS ON-SITE", capturedDialog.BadgeText);
            Assert.Equal("warning", capturedDialog.DialogType);
            Assert.Contains("PERINGATAN GARANSI BERAKHIR", capturedDialog.Message);
            Assert.Contains("BERAKHIR", capturedDialog.DetailNote);
            Assert.True(vm.IsDeviceWarrantyExpired);
            Assert.False(vm.IsDeviceWarrantyActive);
        }

        [Fact]
        public void CustomerApp_LocationViewModel_RemoveAvatar_ShowsWarningConfirmation()
        {
            var apiClient = new ApiClient();
            var user = new UserDto { Id = 1, Name = "Alvin", AvatarUrl = "https://example.com/avatar.jpg" };

            CustomerApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new LocationViewModel(
                apiClient,
                () => null,
                _ => { },
                () => user,
                _ => { },
                dialog => capturedDialog = dialog
            );

            vm.UserAvatarUrl = "https://example.com/avatar.jpg";

            // Trigger RemoveAvatar
            vm.RemoveAvatar();

            Assert.NotNull(capturedDialog);
            Assert.Equal("warning", capturedDialog.DialogType);
            Assert.Contains("Hapus Foto Profil", capturedDialog.Title);

            // Confirm removal
            capturedDialog.Confirm();
            Assert.Null(vm.UserAvatarUrl);
            Assert.Contains("dihapus", vm.SuccessMessage);
        }

        [Fact]
        public void TechnicianApp_Logout_ShowsDangerConfirmModal()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var themeService = new ThemeSettingsService();
            var vm = new TechnicianMainViewModel(apiClient, rustDesk, themeService);

            vm.OnLoginSuccess(new UserDto { Id = 3, Name = "Joko Widodo", Role = "technician" });
            Assert.True(vm.IsAuthenticated);

            vm.Logout();

            Assert.True(vm.IsConfirmDialogOpen);
            Assert.NotNull(vm.ConfirmDialogViewModel);
            Assert.Equal("danger", vm.ConfirmDialogViewModel.DialogType);
            Assert.Contains("Keluar Akun", vm.ConfirmDialogViewModel.Title);

            // Cancel
            vm.ConfirmDialogViewModel.Cancel();
            Assert.False(vm.IsConfirmDialogOpen);
            Assert.True(vm.IsAuthenticated);

            // Confirm
            vm.Logout();
            vm.ConfirmDialogViewModel?.Confirm();
            Assert.False(vm.IsConfirmDialogOpen);
            Assert.False(vm.IsAuthenticated);
        }

        [Fact]
        public async Task TechnicianApp_TicketDetail_EndRemoteSession_ShowsConfirmation()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var ticket = new RepairRequestDto
            {
                Id = 100,
                Type = "remote",
                DamageCategory = "non_physical",
                RemoteSession = new RemoteSessionDto { Id = 5, RustdeskSessionId = "123456" }
            };

            TechnicianApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new TicketDetailViewModel(
                apiClient,
                rustDesk,
                ticket,
                () => { },
                _ => { },
                _ => { },
                dialog => capturedDialog = dialog
            );

            vm.DiagnosisNotes = "Driver Wi-Fi telah diinstal ulang dan lolos pengujian koneksi.";

            // Trigger End remote session as completed
            await vm.EndRemoteSessionAsync("completed");

            Assert.NotNull(capturedDialog);
            Assert.Equal("success", capturedDialog.DialogType);
            Assert.Equal("SUKSES", capturedDialog.BadgeText);
            Assert.Contains("Konfirmasi Selesai Sesi Remote", capturedDialog.Title);
            Assert.Contains("Driver Wi-Fi", capturedDialog.DetailNote);
        }

        [Fact]
        public async Task TechnicianApp_TicketDetail_EscalateToWorkshop_ShowsWarningConfirmation()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var ticket = new RepairRequestDto
            {
                Id = 101,
                Type = "on_site",
                DamageCategory = "physical",
                Device = new DeviceDto { Model = "MacBook Pro M2", SerialNumber = "SN-MBP-99" },
                User = new UserDto { Address = "Jl. Sudirman No. 45 Jakarta" }
            };

            TechnicianApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new TicketDetailViewModel(
                apiClient,
                rustDesk,
                ticket,
                () => { },
                _ => { },
                _ => { },
                dialog => capturedDialog = dialog
            );

            // Trigger Escalate to workshop
            await vm.EscalateToWorkshopAsync();

            Assert.NotNull(capturedDialog);
            Assert.Equal("warning", capturedDialog.DialogType);
            Assert.Equal("WORKSHOP ESCALATION", capturedDialog.BadgeText);
            Assert.Contains("Bawa ke Service Center", capturedDialog.Title);
            Assert.Contains("MacBook Pro M2", capturedDialog.DetailNote);
        }

        [Fact]
        public void TechnicianApp_InvoiceManager_RemoveItem_ShowsConfirmation()
        {
            var apiClient = new ApiClient();
            var ticket = new RepairRequestDto { Id = 105 };

            TechnicianApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new InvoiceManagerViewModel(
                apiClient,
                ticket,
                () => { },
                dialog => capturedDialog = dialog
            );

            var item = new InvoiceItemPayloadDto
            {
                ItemName = "Thermal Paste Arctic MX-4",
                ItemCode = "TP-01",
                Quantity = 1,
                UnitPrice = 75000,
                IsCoveredByWarranty = true
            };

            vm.Items.Add(item);
            Assert.Single(vm.Items);

            // Request Remove
            vm.RemoveItem(item);

            Assert.NotNull(capturedDialog);
            Assert.Equal("danger", capturedDialog.DialogType);
            Assert.Contains("Hapus Item", capturedDialog.Title);

            // Confirm
            capturedDialog.Confirm();
            Assert.Empty(vm.Items);
        }

        [Fact]
        public async Task TechnicianApp_InvoiceManager_SavePayableInvoice_ShowsWarningConfirmation()
        {
            var apiClient = new ApiClient();
            var ticket = new RepairRequestDto { Id = 106 };

            TechnicianApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new InvoiceManagerViewModel(
                apiClient,
                ticket,
                () => { },
                dialog => capturedDialog = dialog
            );

            vm.Items.Clear();
            vm.Items.Add(new InvoiceItemPayloadDto
            {
                ItemName = "Upgrade SSD NVMe 1TB (Non-Garansi)",
                Quantity = 1,
                UnitPrice = 650000,
                IsCoveredByWarranty = false // Customer must pay
            });

            vm.RecalculateTotals();
            Assert.Equal(650000m, vm.TotalPayableAmount);

            // Save
            await vm.SaveInvoiceAsync();

            Assert.NotNull(capturedDialog);
            Assert.Equal("warning", capturedDialog.DialogType);
            Assert.Equal("TAGIHAN BERBAYAR", capturedDialog.BadgeText);
            Assert.Contains("Tagihan Klien", capturedDialog.Title);
            Assert.Contains("Rp 650,000", capturedDialog.DetailNote);
        }

        [Fact]
        public async Task TechnicianApp_WorkshopTracking_AdvanceStep_ShowsConfirmation()
        {
            var apiClient = new ApiClient();
            var ticket = new RepairRequestDto
            {
                Id = 107,
                Tracking = new RepairTrackingDto
                {
                    CurrentStatus = "sedang_diperbaiki"
                }
            };

            TechnicianApp.ViewModels.ConfirmDialogViewModel? capturedDialog = null;

            var vm = new WorkshopTrackingViewModel(
                apiClient,
                ticket,
                () => { },
                dialog => capturedDialog = dialog
            );

            vm.NewNotes = "Unit telah lolos uji benchmark 24 jam tanpa kendala.";

            // Advance step to selesai
            await vm.AdvanceStepAsync("selesai");

            Assert.NotNull(capturedDialog);
            Assert.Equal("success", capturedDialog.DialogType);
            Assert.Equal("WORKSHOP STEPPER", capturedDialog.BadgeText);
            Assert.Contains("Pembaruan Status Workshop", capturedDialog.Title);
            Assert.Contains("benchmark 24 jam", capturedDialog.DetailNote);
        }
    }
}
