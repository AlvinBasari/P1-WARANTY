using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace TechnicianApp.ViewModels
{
    public partial class WorkshopTrackingViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly Action _onClose;

        [ObservableProperty]
        private RepairRequestDto _ticket;

        [ObservableProperty]
        private RepairTrackingDto? _tracking;

        [ObservableProperty]
        private string _currentStatus = "dijemput";

        [ObservableProperty]
        private string _newNotes = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private ObservableCollection<RepairTrackingHistoryDto> _histories = new();

        public bool IsStep1Active => CurrentStatus == "dijemput";
        public bool IsStep2Active => CurrentStatus == "di_service_center";
        public bool IsStep3Active => CurrentStatus == "sedang_diperbaiki";
        public bool IsStep4Active => CurrentStatus == "selesai";
        public bool IsStep5Active => CurrentStatus == "dikembalikan";

        public bool IsStep1Completed => GetStatusIndex(CurrentStatus) >= 0;
        public bool IsStep2Completed => GetStatusIndex(CurrentStatus) >= 1;
        public bool IsStep3Completed => GetStatusIndex(CurrentStatus) >= 2;
        public bool IsStep4Completed => GetStatusIndex(CurrentStatus) >= 3;
        public bool IsStep5Completed => GetStatusIndex(CurrentStatus) >= 4;

        public WorkshopTrackingViewModel(ApiClient apiClient, RepairRequestDto ticket, Action onClose)
        {
            _apiClient = apiClient;
            _ticket = ticket;
            _onClose = onClose;

            if (ticket.Tracking != null)
            {
                ApplyTracking(ticket.Tracking);
            }
            else
            {
                _ = LoadTrackingAsync();
            }
        }

        private static int GetStatusIndex(string status)
        {
            return status.ToLowerInvariant() switch
            {
                "dijemput" => 0,
                "di_service_center" => 1,
                "sedang_diperbaiki" => 2,
                "selesai" => 3,
                "dikembalikan" => 4,
                _ => 0
            };
        }

        private void ApplyTracking(RepairTrackingDto tracking)
        {
            Tracking = tracking;
            CurrentStatus = tracking.CurrentStatus;

            Histories.Clear();
            if (tracking.Histories != null)
            {
                foreach (var h in tracking.Histories)
                {
                    Histories.Add(h);
                }
            }

            OnPropertyChanged(nameof(IsStep1Active));
            OnPropertyChanged(nameof(IsStep2Active));
            OnPropertyChanged(nameof(IsStep3Active));
            OnPropertyChanged(nameof(IsStep4Active));
            OnPropertyChanged(nameof(IsStep5Active));
            OnPropertyChanged(nameof(IsStep1Completed));
            OnPropertyChanged(nameof(IsStep2Completed));
            OnPropertyChanged(nameof(IsStep3Completed));
            OnPropertyChanged(nameof(IsStep4Completed));
            OnPropertyChanged(nameof(IsStep5Completed));
        }

        [RelayCommand]
        public async Task LoadTrackingAsync()
        {
            IsLoading = true;
            StatusMessage = null;

            try
            {
                var detail = await _apiClient.GetRepairTrackingAsync(Ticket.Id);
                if (detail.HasTracking && detail.Tracking != null)
                {
                    ApplyTracking(detail.Tracking);
                }
                else
                {
                    StatusMessage = "Belum ada alur tracking workshop untuk tiket ini.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal memuat tracking: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task AdvanceStepAsync(string targetStatus)
        {
            if (string.IsNullOrEmpty(targetStatus)) return;

            IsLoading = true;
            StatusMessage = null;

            try
            {
                string note = !string.IsNullOrWhiteSpace(NewNotes)
                    ? NewNotes.Trim()
                    : $"Status workshop diperbarui menjadi {targetStatus}.";

                var updated = await _apiClient.UpdateTrackingProgressAsync(Ticket.Id, targetStatus, note);
                ApplyTracking(updated);
                NewNotes = string.Empty;
                StatusMessage = "✓ Status perbaikan berhasil diperbarui!";
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
        public void Close()
        {
            _onClose();
        }
    }
}
