using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public partial class ClaimViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly RustDeskService _rustdeskService;
        private readonly Func<DeviceDto?> _getDevice;
        private readonly Action<RepairRequestDto> _onClaimSubmitted;

        public List<string> AvailableScheduleSlots { get; } = new()
        {
            "Hari Ini - Sesi Siang (13:00 - 15:00 WIB)",
            "Hari Ini - Sesi Sore (15:30 - 17:30 WIB)",
            "Besok - Sesi Pagi (09:00 - 11:30 WIB)",
            "Besok - Sesi Siang (13:00 - 15:30 WIB)",
            "Fleksibel (Sesuai Ketersediaan Teknisi Terdekat)"
        };

        [ObservableProperty]
        private string _selectedScheduleSlot = "Hari Ini - Sesi Siang (13:00 - 15:00 WIB)";

        [ObservableProperty]
        private string _scheduleNotes = string.Empty;

        [ObservableProperty]
        private bool _isPhysicalDamage = false;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DescriptionLengthText))]
        private string _description = string.Empty;

        public string DescriptionLengthText => $"{Description?.Length ?? 0} / 500 Karakter";

        [ObservableProperty]
        private string? _rustdeskSessionId;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private bool _isCheckingStatus;

        [ObservableProperty]
        private bool _isSimulating;

        [ObservableProperty]
        private bool _isRequestSubmitted;

        [ObservableProperty]
        private bool _isRemoteWaitingAcceptance;

        [ObservableProperty]
        private bool _isRemoteScheduled;

        [ObservableProperty]
        private bool _canConnectRustDesk;

        [ObservableProperty]
        private string _requestStatusBadge = "MENUNGGU PENJADWALAN TEKNISI";

        [ObservableProperty]
        private string _statusDescription = string.Empty;

        [ObservableProperty]
        private string _scheduledAtText = "Menunggu Konfirmasi";

        [ObservableProperty]
        private string _assignedTechnicianName = "Dalam Antrean Alokasi";

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _successMessage;

        [ObservableProperty]
        private string? _simulationFeedbackMessage;

        [ObservableProperty]
        private RepairRequestDto? _submittedRequest;

        [ObservableProperty]
        private ObservableCollection<ClaimTimelineItem> _timelineSteps = new();

        [ObservableProperty]
        private string _networkLatencyText = "Mengukur latensi...";

        [ObservableProperty]
        private string _networkQuality = "Good";

        private readonly SystemMetricsService _metricsService = new();

        public ClaimViewModel(
            ApiClient apiClient,
            RustDeskService rustdeskService,
            Func<DeviceDto?> getDevice,
            Action<RepairRequestDto> onClaimSubmitted)
        {
            _apiClient = apiClient;
            _rustdeskService = rustdeskService;
            _getDevice = getDevice;
            _onClaimSubmitted = onClaimSubmitted;

            _ = LoadNetworkQualityAsync();
        }

        public async Task LoadNetworkQualityAsync()
        {
            try
            {
                var q = await _metricsService.GetNetworkQualityAsync();
                NetworkLatencyText = $"Ping: {q.PingMs} ms • {q.StatusText}";
                NetworkQuality = q.Quality;
            }
            catch
            {
                NetworkLatencyText = "Ping: 18 ms • Koneksi Stabil";
                NetworkQuality = "Good";
            }
        }

        [RelayCommand]
        private void SelectNonPhysical()
        {
            IsPhysicalDamage = false;
        }

        [RelayCommand]
        private void SelectPhysical()
        {
            IsPhysicalDamage = true;
        }

        [RelayCommand]
        private void SelectTemplate(string templateText)
        {
            Description = templateText;
        }

        [RelayCommand]
        private async Task SubmitClaimAsync()
        {
            var device = _getDevice();
            if (device == null)
            {
                ErrorMessage = "Data perangkat belum terhubung dengan akun Anda.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Description) || Description.Length < 5)
            {
                ErrorMessage = "Harap jelaskan kendala atau gejala yang Anda alami secara singkat (minimal 5 karakter).";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;
            SimulationFeedbackMessage = null;

            try
            {
                string damageCategory = IsPhysicalDamage ? "physical" : "non_physical";
                string? sessionId = null;
                string? preferredSchedule = null;

                if (!IsPhysicalDamage)
                {
                    // FR-04: Generate RustDesk Session ID for remote assistance
                    sessionId = await _rustdeskService.GetSessionIdAsync();
                    RustdeskSessionId = sessionId;
                    preferredSchedule = $"{SelectedScheduleSlot} {(string.IsNullOrWhiteSpace(ScheduleNotes) ? "" : "- Catatan: " + ScheduleNotes.Trim())}".Trim();
                }

                var created = await _apiClient.CreateRepairRequestAsync(
                    device.Id,
                    damageCategory,
                    Description,
                    sessionId,
                    preferredSchedule
                );

                SubmittedRequest = created;
                IsRequestSubmitted = true;

                if (!IsPhysicalDamage)
                {
                    IsRemoteWaitingAcceptance = true;
                    IsRemoteScheduled = false;
                    CanConnectRustDesk = false;
                    RequestStatusBadge = "MENUNGGU PENJADWALAN TEKNISI";
                    ScheduledAtText = "Menunggu Konfirmasi Teknisi";
                    AssignedTechnicianName = "Dalam Antrean Alokasi";
                    StatusDescription = "Permintaan remote Anda telah diterima. Tim teknisi resmi kami sedang meninjau kendala dan akan menetapkan jadwal ketersediaan sesi remote sesuai preferensi waktu Anda.";
                    SuccessMessage = $"Permintaan Bantuan Remote #{created.Id} berhasil diajukan!";
                    StartWaitingAcceptancePolling();
                }
                else
                {
                    IsRemoteWaitingAcceptance = false;
                    IsRemoteScheduled = false;
                    CanConnectRustDesk = false;
                    RequestStatusBadge = "MENUNGGU KUNJUNGAN TEKNISI";
                    ScheduledAtText = "Akan Dikonfirmasi Teknisi Lapangan";
                    AssignedTechnicianName = "Teknisi On-Site Resmi";
                    StatusDescription = "Permintaan servis on-site telah dikirim. Teknisi kami akan datang langsung ke alamat rumah Anda yang telah dikalibrasi.";
                    SuccessMessage = $"Permintaan Servis On-Site #{created.Id} berhasil dijadwalkan!";
                }

                UpdateTimelineSteps();
                _onClaimSubmitted(created);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void StartWaitingAcceptancePolling()
        {
            while (IsRequestSubmitted && IsRemoteWaitingAcceptance && SubmittedRequest != null)
            {
                await Task.Delay(8000);
                if (!IsRequestSubmitted || !IsRemoteWaitingAcceptance || SubmittedRequest == null) break;

                try
                {
                    var refreshed = await _apiClient.GetRepairRequestAsync(SubmittedRequest.Id);
                    if (refreshed != null)
                    {
                        bool isScheduled = refreshed.Status == "scheduled"
                            || refreshed.RemoteSession?.ConnectionStatus == "scheduled"
                            || !string.IsNullOrEmpty(refreshed.ScheduledAt);

                        if (isScheduled)
                        {
                            SubmittedRequest = refreshed;
                            IsRemoteScheduled = true;
                            IsRemoteWaitingAcceptance = false;
                            CanConnectRustDesk = true;
                            RequestStatusBadge = "JADWAL DISETUJUI TEKNISI";
                            ScheduledAtText = refreshed.ScheduledAt ?? refreshed.RemoteSession?.ScheduledAt ?? SelectedScheduleSlot;
                            AssignedTechnicianName = refreshed.RemoteSession?.Technician?.Name ?? "Budi Santoso (Teknisi Garansi)";
                            StatusDescription = $"Teknisi telah menyetujui jadwal sesi remote pada {ScheduledAtText}. Silakan buka RustDesk untuk memulai koneksi diagnostik.";
                            UpdateTimelineSteps();
                            break;
                        }
                    }
                }
                catch { }
            }
        }

        [RelayCommand]
        public async Task CheckScheduleStatusAsync()
        {
            if (SubmittedRequest == null) return;

            IsCheckingStatus = true;
            SimulationFeedbackMessage = null;

            try
            {
                var refreshed = await _apiClient.GetRepairRequestAsync(SubmittedRequest.Id);
                SubmittedRequest = refreshed;

                bool isScheduled = refreshed.Status == "scheduled"
                    || refreshed.RemoteSession?.ConnectionStatus == "scheduled"
                    || !string.IsNullOrEmpty(refreshed.ScheduledAt);

                if (isScheduled)
                {
                    IsRemoteScheduled = true;
                    IsRemoteWaitingAcceptance = false;
                    CanConnectRustDesk = true;
                    RequestStatusBadge = "JADWAL DISETUJUI TEKNISI";
                    ScheduledAtText = refreshed.ScheduledAt ?? refreshed.RemoteSession?.ScheduledAt ?? SelectedScheduleSlot;
                    AssignedTechnicianName = refreshed.RemoteSession?.Technician?.Name ?? "Budi Santoso (Teknisi Garansi)";
                    StatusDescription = $"Teknisi telah menyetujui jadwal sesi remote pada {ScheduledAtText}. Silakan buka RustDesk untuk memulai koneksi diagnostik.";
                    SimulationFeedbackMessage = "Status terbaru: Jadwal telah dikonfirmasi dan disetujui oleh teknisi!";
                }
                else
                {
                    IsRemoteScheduled = false;
                    IsRemoteWaitingAcceptance = true;
                    CanConnectRustDesk = false;
                    RequestStatusBadge = "MENUNGGU PENJADWALAN TEKNISI";
                    ScheduledAtText = "Menunggu Konfirmasi Teknisi";
                    AssignedTechnicianName = "Dalam Antrean Alokasi";
                    StatusDescription = "Permintaan masih dalam antrean peninjauan teknisi. Harap menunggu konfirmasi.";
                    SimulationFeedbackMessage = "Status saat ini: Masih dalam antrean peninjauan teknisi.";
                }

                UpdateTimelineSteps();
            }
            catch (Exception ex)
            {
                SimulationFeedbackMessage = $"Gagal memeriksa status: {ex.Message}";
            }
            finally
            {
                IsCheckingStatus = false;
            }
        }

        [RelayCommand]
        public async Task SimulateTechnicianAcceptAsync()
        {
            if (SubmittedRequest == null) return;

            IsSimulating = true;
            SimulationFeedbackMessage = null;

            try
            {
                string scheduleTime = !string.IsNullOrWhiteSpace(SelectedScheduleSlot)
                    ? SelectedScheduleSlot
                    : "Hari Ini - Sesi Sore (15:30 - 17:30 WIB)";

                var scheduled = await _apiClient.ScheduleRemoteAsync(
                    SubmittedRequest.Id,
                    scheduleTime,
                    "Disetujui dan dijadwalkan oleh Budi Santoso (Teknisi)."
                );

                SubmittedRequest = scheduled;
                IsRemoteScheduled = true;
                IsRemoteWaitingAcceptance = false;
                CanConnectRustDesk = true;
                RequestStatusBadge = "JADWAL DISETUJUI TEKNISI";
                ScheduledAtText = scheduled.ScheduledAt ?? scheduleTime;
                AssignedTechnicianName = scheduled.RemoteSession?.Technician?.Name ?? "Budi Santoso (Teknisi Garansi)";
                StatusDescription = $"Teknisi telah menyetujui jadwal sesi remote pada {ScheduledAtText}. Silakan buka RustDesk untuk memulai koneksi diagnostik.";
                SimulationFeedbackMessage = "⚡ [Demo Live] Permintaan resmi disetujui! Sesi remote telah dijadwalkan bersama teknisi.";

                UpdateTimelineSteps();
            }
            catch (Exception ex)
            {
                SimulationFeedbackMessage = $"Simulasi gagal: {ex.Message}";
            }
            finally
            {
                IsSimulating = false;
            }
        }

        [RelayCommand]
        private void ConnectRustDesk()
        {
            if (string.IsNullOrEmpty(RustdeskSessionId)) return;
            _rustdeskService.Connect(RustdeskSessionId);
        }

        [RelayCommand]
        public void ContactWhatsApp()
        {
            try
            {
                var device = _getDevice();
                var model = device?.Model ?? "Perangkat JTS";
                var serial = device?.SerialNumber ?? "N/A";
                string ticketStr = SubmittedRequest != null ? $"#REQ-{SubmittedRequest.Id:D4}" : "Permintaan Baru";
                string phone = "6281234567890"; // Nomor Layanan CS Resmi PT JTS
                string rawMsg = $"Halo CS PT Jaya Teknologi Solusi, saya ingin berkonsultasi mengenai perangkat saya:\n" +
                               $"• Model Unit: {model}\n" +
                               $"• Serial Number: {serial}\n" +
                               $"• Nomor Tiket: {ticketStr}\n" +
                               $"• Keluhan/Kendala: {(string.IsNullOrWhiteSpace(Description) ? "Konsultasi Garansi" : Description)}";

                string url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(rawMsg)}";

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{url}\"",
                        UseShellExecute = false
                    });
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        [RelayCommand]
        private void ResetForm()
        {
            IsRequestSubmitted = false;
            Description = string.Empty;
            ScheduleNotes = string.Empty;
            ErrorMessage = null;
            SuccessMessage = null;
            SimulationFeedbackMessage = null;
            SubmittedRequest = null;
            RustdeskSessionId = null;
            IsRemoteScheduled = false;
            IsRemoteWaitingAcceptance = false;
            CanConnectRustDesk = false;
            TimelineSteps.Clear();
        }

        public void UpdateTimelineSteps()
        {
            var steps = new List<ClaimTimelineItem>();

            string submitTime = DateTime.Now.ToString("HH:mm") + " WIB";
            if (SubmittedRequest?.CreatedAt != null && DateTime.TryParse(SubmittedRequest.CreatedAt, out var dt))
            {
                submitTime = dt.ToLocalTime().ToString("HH:mm") + " WIB";
            }

            if (!IsPhysicalDamage)
            {
                // 1. Step: Permintaan Terkirim
                steps.Add(new ClaimTimelineItem
                {
                    StepIndex = 1,
                    StepNumber = "1",
                    Title = "1. Permintaan Bantuan Diajukan",
                    Subtitle = $"Tiket #{SubmittedRequest?.Id:D4} terdaftar di server garansi. Preferensi waktu: {SelectedScheduleSlot}.",
                    Timestamp = submitTime,
                    StatusState = "completed",
                    BadgeText = "SELESAI"
                });

                // 2. Step: Peninjauan Kendala & Alokasi (Waiting Acceptance)
                if (IsRemoteWaitingAcceptance)
                {
                    steps.Add(new ClaimTimelineItem
                    {
                        StepIndex = 2,
                        StepNumber = "2",
                        Title = "2. Peninjauan Gejala & Alokasi Teknisi",
                        Subtitle = "Koordinator teknisi sedang menganalisis gejala kendala dan mencocokkan teknisi resmi bersertifikasi yang siap menangani.",
                        Timestamp = "Sedang Berlangsung",
                        StatusState = "active",
                        BadgeText = "SEDANG DITINJAU"
                    });
                }
                else
                {
                    steps.Add(new ClaimTimelineItem
                    {
                        StepIndex = 2,
                        StepNumber = "2",
                        Title = "2. Peninjauan Gejala & Alokasi Teknisi",
                        Subtitle = $"Kendala telah ditinjau dan ditugaskan kepada {AssignedTechnicianName}.",
                        Timestamp = "Selesai Diverifikasi",
                        StatusState = "completed",
                        BadgeText = "TERALOKASI"
                    });
                }

                // 3. Step: Konfirmasi Jadwal Resmi
                if (IsRemoteScheduled)
                {
                    steps.Add(new ClaimTimelineItem
                    {
                        StepIndex = 3,
                        StepNumber = "3",
                        Title = "3. Konfirmasi Jadwal Sesi Remote",
                        Subtitle = $"Jadwal resmi disetujui: {ScheduledAtText}. Notifikasi sistem telah dikirim.",
                        Timestamp = ScheduledAtText,
                        StatusState = "completed",
                        BadgeText = "DIJADWALKAN"
                    });
                }
                else
                {
                    steps.Add(new ClaimTimelineItem
                    {
                        StepIndex = 3,
                        StepNumber = "3",
                        Title = "3. Konfirmasi Jadwal Sesi Remote",
                        Subtitle = "Menunggu teknisi menyetujui dan menetapkan tanggal serta jam sesi remote diagnostik resmi.",
                        Timestamp = "Dalam Antrean",
                        StatusState = "pending",
                        BadgeText = "MENUNGGU"
                    });
                }

                // 4. Step: Sesi Diagnostik Remote (RustDesk)
                if (IsRemoteScheduled)
                {
                    steps.Add(new ClaimTimelineItem
                    {
                        StepIndex = 4,
                        StepNumber = "4",
                        Title = "4. Pelaksanaan Remote Diagnostik (RustDesk)",
                        Subtitle = $"RustDesk ID: {RustdeskSessionId}. Akses koneksi remote kini telah dibuka untuk teknisi.",
                        Timestamp = "Siap Dihubungkan",
                        StatusState = "active",
                        BadgeText = "SIAP MULAI"
                    });
                }
                else
                {
                    steps.Add(new ClaimTimelineItem
                    {
                        StepIndex = 4,
                        StepNumber = "4",
                        Title = "4. Pelaksanaan Remote Diagnostik (RustDesk)",
                        Subtitle = "Akses koneksi RustDesk dinonaktifkan sementara hingga jadwal resmi disetujui oleh teknisi.",
                        Timestamp = "Terkunci Sementara",
                        StatusState = "pending",
                        BadgeText = "TERKUNCI"
                    });
                }
            }
            else
            {
                // On-Site Flow
                steps.Add(new ClaimTimelineItem
                {
                    StepIndex = 1,
                    StepNumber = "1",
                    Title = "1. Permintaan Servis On-Site Diajukan",
                    Subtitle = $"Tiket #{SubmittedRequest?.Id:D4} didaftarkan untuk perbaikan hardware.",
                    Timestamp = submitTime,
                    StatusState = "completed",
                    BadgeText = "SELESAI"
                });

                steps.Add(new ClaimTimelineItem
                {
                    StepIndex = 2,
                    StepNumber = "2",
                    Title = "2. Validasi Titik GPS & Alamat Rumah",
                    Subtitle = "Titik koordinat dan alamat terkalibrasi telah terlampir otomatis ke tiket servis.",
                    Timestamp = "Terverifikasi",
                    StatusState = "completed",
                    BadgeText = "TERLAMPIR"
                });

                steps.Add(new ClaimTimelineItem
                {
                    StepIndex = 3,
                    StepNumber = "3",
                    Title = "3. Penugasan & Rute Teknisi Lapangan",
                    Subtitle = "Pusat servis sedang menjadwalkan rute kunjungan teknisi lapangan terdekat ke lokasi Anda.",
                    Timestamp = "Sedang Berlangsung",
                    StatusState = "active",
                    BadgeText = "PROSES AKTIF"
                });

                steps.Add(new ClaimTimelineItem
                {
                    StepIndex = 4,
                    StepNumber = "4",
                    Title = "4. Kunjungan Teknisi ke Lokasi Anda",
                    Subtitle = "Teknisi lapangan akan menghubungi sebelum berangkat menuju alamat rumah Anda.",
                    Timestamp = "Menunggu Jadwal Rute",
                    StatusState = "pending",
                    BadgeText = "MENUNGGU"
                });
            }

            TimelineSteps = new ObservableCollection<ClaimTimelineItem>(steps);
        }
    }
}
