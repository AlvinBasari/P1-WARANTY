using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace TechnicianApp.ViewModels
{
    public partial class TicketQueueViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly Action<RepairRequestDto> _onOpenTicketDetail;
        private readonly Action<RepairRequestDto> _onQuickRemote;
        private readonly Action<RepairRequestDto> _onQuickTracking;
        private readonly Action<RepairRequestDto> _onQuickInvoice;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private string _selectedTab = "all"; // all, remote, onsite, workshop, completed

        [ObservableProperty]
        private string _selectedStatusFilter = "all"; // all, pending, scheduled, in_progress, completed

        [ObservableProperty]
        private string _searchKeyword = string.Empty;

        [ObservableProperty]
        private int _totalCount;

        [ObservableProperty]
        private int _remoteCount;

        [ObservableProperty]
        private int _onSiteCount;

        [ObservableProperty]
        private int _workshopCount;

        [ObservableProperty]
        private int _completedCount;

        private List<RepairRequestDto> _allTickets = new();

        [ObservableProperty]
        private ObservableCollection<RepairRequestDto> _filteredTickets = new();

        public bool IsTabAll => SelectedTab == "all";
        public bool IsTabRemote => SelectedTab == "remote";
        public bool IsTabOnSite => SelectedTab == "onsite";
        public bool IsTabWorkshop => SelectedTab == "workshop";
        public bool IsTabCompleted => SelectedTab == "completed";

        public TicketQueueViewModel(
            ApiClient apiClient,
            Action<RepairRequestDto> onOpenTicketDetail,
            Action<RepairRequestDto> onQuickRemote,
            Action<RepairRequestDto> onQuickTracking,
            Action<RepairRequestDto> onQuickInvoice)
        {
            _apiClient = apiClient;
            _onOpenTicketDetail = onOpenTicketDetail;
            _onQuickRemote = onQuickRemote;
            _onQuickTracking = onQuickTracking;
            _onQuickInvoice = onQuickInvoice;

            _ = LoadTicketsAsync();
        }

        partial void OnSearchKeywordChanged(string value)
        {
            ApplyFilter();
        }

        [RelayCommand]
        public async Task LoadTicketsAsync()
        {
            IsLoading = true;
            StatusMessage = null;

            try
            {
                var tickets = await _apiClient.GetRepairRequestsListAsync();
                _allTickets = tickets ?? new List<RepairRequestDto>();

                UpdateCounts();
                ApplyFilter();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal memuat antrean tiket: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateCounts()
        {
            TotalCount = _allTickets.Count;
            RemoteCount = _allTickets.Count(t => t.Type.Equals("remote", StringComparison.OrdinalIgnoreCase) ||
                                                t.DamageCategory.Equals("non_physical", StringComparison.OrdinalIgnoreCase));
            OnSiteCount = _allTickets.Count(t => (t.Type.Equals("on_site", StringComparison.OrdinalIgnoreCase) ||
                                                 t.DamageCategory.Equals("physical", StringComparison.OrdinalIgnoreCase)) &&
                                                !t.NeedsOfficeRepair);
            WorkshopCount = _allTickets.Count(t => t.NeedsOfficeRepair || t.Tracking != null);
            CompletedCount = _allTickets.Count(t => t.Status.Equals("completed", StringComparison.OrdinalIgnoreCase));
        }

        [RelayCommand]
        public void SelectTab(string tab)
        {
            SelectedTab = tab;
            OnPropertyChanged(nameof(IsTabAll));
            OnPropertyChanged(nameof(IsTabRemote));
            OnPropertyChanged(nameof(IsTabOnSite));
            OnPropertyChanged(nameof(IsTabWorkshop));
            OnPropertyChanged(nameof(IsTabCompleted));

            ApplyFilter();
        }

        [RelayCommand]
        public void SelectStatusFilter(string status)
        {
            SelectedStatusFilter = status;
            ApplyFilter();
        }

        [RelayCommand]
        public void ResetFilter()
        {
            SearchKeyword = string.Empty;
            SelectedStatusFilter = "all";
            SelectTab("all");
        }

        public void ApplyFilter()
        {
            var query = _allTickets.AsEnumerable();

            // Apply Tab Category Filter
            query = SelectedTab switch
            {
                "remote" => query.Where(t => t.Type.Equals("remote", StringComparison.OrdinalIgnoreCase) ||
                                            t.DamageCategory.Equals("non_physical", StringComparison.OrdinalIgnoreCase)),
                "onsite" => query.Where(t => (t.Type.Equals("on_site", StringComparison.OrdinalIgnoreCase) ||
                                             t.DamageCategory.Equals("physical", StringComparison.OrdinalIgnoreCase)) &&
                                            !t.NeedsOfficeRepair),
                "workshop" => query.Where(t => t.NeedsOfficeRepair || t.Tracking != null),
                "completed" => query.Where(t => t.Status.Equals("completed", StringComparison.OrdinalIgnoreCase)),
                _ => query
            };

            // Apply Status Filter
            if (SelectedStatusFilter != "all")
            {
                query = query.Where(t => t.Status.Equals(SelectedStatusFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Apply Keyword Search
            if (!string.IsNullOrWhiteSpace(SearchKeyword))
            {
                string kw = SearchKeyword.Trim().ToLowerInvariant();
                query = query.Where(t =>
                    t.Id.ToString().Contains(kw) ||
                    (t.User?.Name != null && t.User.Name.ToLowerInvariant().Contains(kw)) ||
                    (t.Device?.Model != null && t.Device.Model.ToLowerInvariant().Contains(kw)) ||
                    (t.Device?.SerialNumber != null && t.Device.SerialNumber.ToLowerInvariant().Contains(kw)) ||
                    (t.Description != null && t.Description.ToLowerInvariant().Contains(kw))
                );
            }

            FilteredTickets.Clear();
            foreach (var ticket in query.OrderByDescending(t => t.Id))
            {
                FilteredTickets.Add(ticket);
            }
        }

        [RelayCommand]
        public void OpenDetail(RepairRequestDto ticket)
        {
            if (ticket != null)
            {
                _onOpenTicketDetail(ticket);
            }
        }

        [RelayCommand]
        public void QuickRemote(RepairRequestDto ticket)
        {
            if (ticket != null)
            {
                _onQuickRemote(ticket);
            }
        }

        [RelayCommand]
        public void QuickTracking(RepairRequestDto ticket)
        {
            if (ticket != null)
            {
                _onQuickTracking(ticket);
            }
        }

        [RelayCommand]
        public void QuickInvoice(RepairRequestDto ticket)
        {
            if (ticket != null)
            {
                _onQuickInvoice(ticket);
            }
        }
    }
}
