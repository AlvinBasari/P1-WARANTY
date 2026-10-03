using System.Text.Json;
using CustomerApp.ViewModels;
using SharedCore.Models;
using SharedCore.Services;
using Xunit;

namespace DesktopApp.Tests
{
    public class ClaimSchedulingTests
    {
        [Fact]
        public void ClaimViewModel_DefaultState_HasScheduleSlotsAndRemoteMode()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var vm = new ClaimViewModel(
                apiClient,
                rustDesk,
                () => new DeviceDto { Id = 1 },
                _ => { }
            );

            Assert.False(vm.IsPhysicalDamage);
            Assert.NotEmpty(vm.AvailableScheduleSlots);
            Assert.Contains(vm.SelectedScheduleSlot, vm.AvailableScheduleSlots);
            Assert.False(vm.IsRequestSubmitted);
            Assert.False(vm.CanConnectRustDesk);
            Assert.False(vm.IsRemoteWaitingAcceptance);
            Assert.False(vm.IsRemoteScheduled);
            Assert.Empty(vm.TimelineSteps);
        }

        [Fact]
        public void ClaimViewModel_SwitchChannels_UpdatesIsPhysicalDamage()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var vm = new ClaimViewModel(apiClient, rustDesk, () => null, _ => { });

            vm.SelectPhysicalCommand.Execute(null);
            Assert.True(vm.IsPhysicalDamage);

            vm.SelectNonPhysicalCommand.Execute(null);
            Assert.False(vm.IsPhysicalDamage);
        }

        [Fact]
        public void ClaimViewModel_SelectTemplate_SetsDescription()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var vm = new ClaimViewModel(apiClient, rustDesk, () => null, _ => { });

            vm.SelectTemplateCommand.Execute("Layar berkedip");
            Assert.Equal("Layar berkedip", vm.Description);
        }

        [Fact]
        public void ClaimViewModel_ResetForm_RestoresInitialPropertiesAndClearsTimeline()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var vm = new ClaimViewModel(apiClient, rustDesk, () => null, _ => { })
            {
                Description = "Kendala pada trackpad",
                ScheduleNotes = "Tolong hubungi via WA",
                IsRequestSubmitted = true,
                IsRemoteScheduled = true,
                CanConnectRustDesk = true
            };

            vm.UpdateTimelineSteps();
            Assert.NotEmpty(vm.TimelineSteps);

            vm.ResetFormCommand.Execute(null);

            Assert.False(vm.IsRequestSubmitted);
            Assert.Empty(vm.Description);
            Assert.Empty(vm.ScheduleNotes);
            Assert.False(vm.IsRemoteScheduled);
            Assert.False(vm.CanConnectRustDesk);
            Assert.Empty(vm.TimelineSteps);
        }

        [Fact]
        public void ClaimViewModel_UpdateTimelineSteps_WaitingAcceptance_SetsActiveStep2()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var vm = new ClaimViewModel(apiClient, rustDesk, () => null, _ => { })
            {
                IsPhysicalDamage = false,
                IsRequestSubmitted = true,
                IsRemoteWaitingAcceptance = true,
                IsRemoteScheduled = false,
                RustdeskSessionId = "948123456",
                SubmittedRequest = new RepairRequestDto { Id = 101, Status = "pending" }
            };

            vm.UpdateTimelineSteps();

            Assert.Equal(4, vm.TimelineSteps.Count);

            // Step 1: Submission
            Assert.True(vm.TimelineSteps[0].IsCompleted);
            Assert.Equal("SELESAI", vm.TimelineSteps[0].BadgeText);

            // Step 2: In Review (Waiting Acceptance)
            Assert.True(vm.TimelineSteps[1].IsActive);
            Assert.Equal("SEDANG DITINJAU", vm.TimelineSteps[1].BadgeText);
            Assert.Equal("#F59E0B", vm.TimelineSteps[1].BorderColorHex);

            // Step 3: Pending Scheduling
            Assert.True(vm.TimelineSteps[2].IsPending);
            Assert.Equal("MENUNGGU", vm.TimelineSteps[2].BadgeText);

            // Step 4: Locked RustDesk
            Assert.True(vm.TimelineSteps[3].IsPending);
            Assert.Equal("TERKUNCI", vm.TimelineSteps[3].BadgeText);
        }

        [Fact]
        public void ClaimViewModel_UpdateTimelineSteps_Scheduled_SetsCompletedStepsAndActiveStep4()
        {
            var apiClient = new ApiClient();
            var rustDesk = new RustDeskService();
            var vm = new ClaimViewModel(apiClient, rustDesk, () => null, _ => { })
            {
                IsPhysicalDamage = false,
                IsRequestSubmitted = true,
                IsRemoteWaitingAcceptance = false,
                IsRemoteScheduled = true,
                CanConnectRustDesk = true,
                ScheduledAtText = "Hari Ini 14:00 WIB",
                AssignedTechnicianName = "Budi Santoso",
                RustdeskSessionId = "948123456",
                SubmittedRequest = new RepairRequestDto { Id = 101, Status = "scheduled" }
            };

            vm.UpdateTimelineSteps();

            Assert.Equal(4, vm.TimelineSteps.Count);

            // Step 1: Submission
            Assert.True(vm.TimelineSteps[0].IsCompleted);

            // Step 2: Allocation
            Assert.True(vm.TimelineSteps[1].IsCompleted);
            Assert.Equal("TERALOKASI", vm.TimelineSteps[1].BadgeText);

            // Step 3: Scheduled
            Assert.True(vm.TimelineSteps[2].IsCompleted);
            Assert.Equal("DIJADWALKAN", vm.TimelineSteps[2].BadgeText);
            Assert.Equal("#22C55E", vm.TimelineSteps[2].BorderColorHex);

            // Step 4: RustDesk Ready
            Assert.True(vm.TimelineSteps[3].IsActive);
            Assert.Equal("SIAP MULAI", vm.TimelineSteps[3].BadgeText);
        }

        [Fact]
        public void RepairRequestDto_SupportsSchedulingFieldsSerialization()
        {
            var json = @"{
                ""id"": 42,
                ""preferred_schedule"": ""Hari Ini - Sesi Siang (13:00 - 15:00 WIB)"",
                ""scheduled_at"": ""2026-09-04 14:00:00"",
                ""status"": ""scheduled"",
                ""remote_session"": {
                    ""id"": 10,
                    ""rustdesk_session_id"": ""948123456"",
                    ""scheduled_at"": ""2026-09-04 14:00:00"",
                    ""connection_status"": ""scheduled"",
                    ""technician"": {
                        ""id"": 2,
                        ""name"": ""Budi Santoso (Teknisi)""
                    }
                }
            }";

            var dto = JsonSerializer.Deserialize<RepairRequestDto>(json);

            Assert.NotNull(dto);
            Assert.Equal(42, dto.Id);
            Assert.Equal("Hari Ini - Sesi Siang (13:00 - 15:00 WIB)", dto.PreferredSchedule);
            Assert.Equal("2026-09-04 14:00:00", dto.ScheduledAt);
            Assert.Equal("scheduled", dto.Status);
            Assert.NotNull(dto.RemoteSession);
            Assert.Equal("scheduled", dto.RemoteSession.ConnectionStatus);
            Assert.NotNull(dto.RemoteSession.Technician);
            Assert.Equal("Budi Santoso (Teknisi)", dto.RemoteSession.Technician.Name);
        }
    }
}
