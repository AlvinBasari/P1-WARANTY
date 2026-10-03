using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace TechnicianApp.ViewModels
{
    public partial class TicketDetailViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly RustDeskService _rustDeskService;
        private readonly Action _onBack;
        private readonly Action<RepairRequestDto> _onOpenTracking;
        private readonly Action<RepairRequestDto> _onOpenInvoice;

        [ObservableProperty]
        private RepairRequestDto _ticket;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _statusMessage;

        // Schedule Form
        [ObservableProperty]
        private string _newScheduleInput = string.Empty;

        [ObservableProperty]
        private string _scheduleNotesInput = "Jadwal remote telah dikonfirmasi oleh teknisi.";

        // Session End Form
        [ObservableProperty]
        private string _diagnosisNotes = string.Empty;

        public bool IsRemoteTicket => Ticket.Type.Equals("remote", StringComparison.OrdinalIgnoreCase) ||
                                     Ticket.DamageCategory.Equals("non_physical", StringComparison.OrdinalIgnoreCase);

        public bool IsOnSiteTicket => !IsRemoteTicket;

        public bool HasRustdeskSession => !string.IsNullOrEmpty(Ticket.RemoteSession?.RustdeskSessionId);
        public string RustdeskSessionId => Ticket.RemoteSession?.RustdeskSessionId ?? "-";

        public bool HasLocation => Ticket.LocationLat.HasValue && Ticket.LocationLng.HasValue;
        public string LocationCoordinatesText => HasLocation ? $"{Ticket.LocationLat:F5}, {Ticket.LocationLng:F5}" : "Lokasi belum terkalibrasi";
        public string LocationLabelText => Ticket.Device?.LocationLabel ?? "Alamat Klien";

        public string CustomerName => Ticket.User?.Name ?? "Pelanggan";
        public string CustomerEmail => Ticket.User?.Email ?? "-";
        public string CustomerPhone => Ticket.User?.Phone ?? "-";
        public string CustomerWhatsApp => Ticket.User?.WhatsappNumber ?? Ticket.User?.Phone ?? "-";
        public string CustomerAddress => Ticket.User?.Address ?? "-";

        public string DeviceModel => Ticket.Device?.Model ?? "-";
        public string DeviceSerial => Ticket.Device?.SerialNumber ?? "-";
        public string WarrantyStatusText => Ticket.Device?.Warranty?.Status?.ToUpperInvariant() ?? "AKTIF";
        public string WarrantyExpiryText => Ticket.Device?.Warranty?.WarrantyEnd ?? "1 Tahun Jaminan";

        public TicketDetailViewModel(
            ApiClient apiClient,
            RustDeskService rustDeskService,
            RepairRequestDto ticket,
            Action onBack,
            Action<RepairRequestDto> onOpenTracking,
            Action<RepairRequestDto> onOpenInvoice)
        {
            _apiClient = apiClient;
            _rustDeskService = rustDeskService;
            _ticket = ticket;
            _onBack = onBack;
            _onOpenTracking = onOpenTracking;
            _onOpenInvoice = onOpenInvoice;

            NewScheduleInput = ticket.PreferredSchedule ?? DateTime.Now.AddHours(1).ToString("yyyy-MM-dd HH:00");
        }

        [RelayCommand]
        public async Task ScheduleRemoteSessionAsync()
        {
            if (string.IsNullOrWhiteSpace(NewScheduleInput))
            {
                StatusMessage = "Harap masukkan waktu jadwal remote.";
                return;
            }

            IsLoading = true;
            StatusMessage = null;

            try
            {
                var updated = await _apiClient.ScheduleRemoteAsync(
                    Ticket.Id,
                    NewScheduleInput.Trim(),
                    ScheduleNotesInput.Trim());

                Ticket = updated;
                OnPropertyChanged(nameof(Ticket));
                StatusMessage = "✓ Sesi remote berhasil disetujui & dijadwalkan!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal menjadwalkan sesi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void LaunchRustDesk()
        {
            if (string.IsNullOrEmpty(RustdeskSessionId) || RustdeskSessionId == "-")
            {
                StatusMessage = "Session ID RustDesk tidak ditemukan pada tiket ini.";
                return;
            }

            StatusMessage = $"Membuka RustDesk untuk sesi: {RustdeskSessionId}...";
            bool launched = _rustDeskService.Connect(RustdeskSessionId);

            if (launched)
            {
                StatusMessage = $"✓ RustDesk aktif untuk sesi ID: {RustdeskSessionId}";
            }
            else
            {
                StatusMessage = $"⚠️ Mode simulasi RustDesk aktif: terhubung ke sesi {RustdeskSessionId}";
            }

            // Also start session audit in backend if session exists
            if (Ticket.RemoteSession != null && Ticket.RemoteSession.Id > 0)
            {
                _ = _apiClient.StartRemoteSessionAsync(Ticket.RemoteSession.Id);
            }
        }

        [RelayCommand]
        public async Task EndRemoteSessionAsync(string outcome)
        {
            if (Ticket.RemoteSession == null || Ticket.RemoteSession.Id == 0)
            {
                StatusMessage = "Tidak ada sesi remote aktif yang dapat diakhiri.";
                return;
            }

            IsLoading = true;
            StatusMessage = null;

            try
            {
                string status = outcome.Equals("failed", StringComparison.OrdinalIgnoreCase) ? "failed" : "completed";
                string note = !string.IsNullOrWhiteSpace(DiagnosisNotes)
                    ? DiagnosisNotes.Trim()
                    : $"Sesi perbaikan jarak jauh selesai dengan status: {status}.";

                var session = await _apiClient.EndRemoteSessionAsync(Ticket.RemoteSession.Id, status, note);
                Ticket.RemoteSession = session;
                Ticket.Status = status == "completed" ? "completed" : "in_progress";
                OnPropertyChanged(nameof(Ticket));

                StatusMessage = $"✓ Sesi remote telah diakhiri ({status}). Log diagnosa tersimpan.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal mengakhiri sesi: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void OpenGoogleMaps()
        {
            if (!HasLocation)
            {
                StatusMessage = "Koordinat GPS belum tersedia.";
                return;
            }

            try
            {
                string url = $"https://www.google.com/maps?q={Ticket.LocationLat},{Ticket.LocationLng}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                StatusMessage = "Membuka Google Maps di peramban web...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal membuka peta: {ex.Message}";
            }
        }

        [RelayCommand]
        public void OpenWhatsApp()
        {
            string phone = CustomerWhatsApp.Replace("+", "").Replace("-", "").Replace(" ", "").Trim();
            if (string.IsNullOrEmpty(phone) || phone == "-")
            {
                StatusMessage = "Nomor WhatsApp pelanggan tidak valid.";
                return;
            }

            if (phone.StartsWith("08"))
            {
                phone = "628" + phone[2..];
            }

            try
            {
                string text = Uri.EscapeDataString($"Halo Kak {CustomerName}, saya teknisi dari PT JTS terkait perbaikan laptop {DeviceModel} (Tiket #{Ticket.Id}).");
                string url = $"https://wa.me/{phone}?text={text}";
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                StatusMessage = "Membuka obrolan WhatsApp pelanggan...";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal membuka WhatsApp: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task MarkCompletedOnSiteAsync()
        {
            IsLoading = true;
            StatusMessage = null;

            try
            {
                var updated = await _apiClient.MarkOfficeRepairAsync(Ticket.Id, false, "Perbaikan selesai ditangani langsung di lokasi pelanggan.");
                Ticket = updated;
                OnPropertyChanged(nameof(Ticket));
                StatusMessage = "✓ Status ditandai: Selesai di Tempat!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal memperbarui status: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task EscalateToWorkshopAsync()
        {
            IsLoading = true;
            StatusMessage = null;

            try
            {
                var updated = await _apiClient.MarkOfficeRepairAsync(
                    Ticket.Id,
                    true,
                    "Unit tidak dapat diperbaiki di lokasi dan dibawa ke Workshop / Service Center untuk penanganan lebih lanjut.");

                Ticket = updated;
                OnPropertyChanged(nameof(Ticket));
                StatusMessage = "✓ Unit berhasil dialihkan ke Workshop / Service Center!";
                _onOpenTracking(Ticket);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal mengalihkan tiket: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void OpenWorkshopTracking()
        {
            _onOpenTracking(Ticket);
        }

        [RelayCommand]
        public void OpenInvoiceManager()
        {
            _onOpenInvoice(Ticket);
        }

        [RelayCommand]
        public void BackToQueue()
        {
            _onBack();
        }
    }
}
