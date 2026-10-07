using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace TechnicianApp.ViewModels
{
    public partial class TechnicianMainViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly RustDeskService _rustDeskService;
        private readonly ThemeSettingsService _themeService;

        [ObservableProperty]
        private UserDto? _currentUser;

        [ObservableProperty]
        private bool _isAuthenticated;

        [ObservableProperty]
        private string _currentPage = "auth"; // auth, queue, detail

        [ObservableProperty]
        private bool _isDarkMode = true;

        [ObservableProperty]
        private string _serverStatusText = "Server Siap";

        [ObservableProperty]
        private bool _isServerOnline = true;

        [ObservableProperty]
        private TechnicianAuthViewModel _authViewModel;

        [ObservableProperty]
        private TicketQueueViewModel? _queueViewModel;

        [ObservableProperty]
        private TicketDetailViewModel? _detailViewModel;

        // Modal Dialog ViewModels
        [ObservableProperty]
        private WorkshopTrackingViewModel? _trackingDialogViewModel;

        [ObservableProperty]
        private InvoiceManagerViewModel? _invoiceDialogViewModel;

        [ObservableProperty]
        private bool _isTrackingDialogOpen;

        [ObservableProperty]
        private bool _isInvoiceDialogOpen;

        [ObservableProperty]
        private ConfirmDialogViewModel? _confirmDialogViewModel;

        [ObservableProperty]
        private bool _isConfirmDialogOpen;

        public bool IsAuthPage => CurrentPage == "auth";
        public bool IsQueuePage => CurrentPage == "queue";
        public bool IsDetailPage => CurrentPage == "detail";

        public string TechnicianName => CurrentUser?.Name ?? "Teknisi PT JTS";
        public string TechnicianEmail => CurrentUser?.Email ?? "technician@warranty.com";
        public string TechnicianRole => "STAFF TEKNISI RESMI";

        public TechnicianMainViewModel(ApiClient apiClient, RustDeskService rustDeskService, ThemeSettingsService themeService)
        {
            _apiClient = apiClient;
            _rustDeskService = rustDeskService;
            _themeService = themeService;

            var saved = _themeService.GetSavedTheme();
            IsDarkMode = !saved.Equals("Light", StringComparison.OrdinalIgnoreCase);

            _authViewModel = new TechnicianAuthViewModel(_apiClient, OnLoginSuccess);
        }

        public void ShowConfirmDialog(ConfirmDialogViewModel dialog)
        {
            var origConfirm = dialog.ConfirmAction;
            var origCancel = dialog.CancelAction;

            dialog.SetCallbacks(
                onConfirm: () =>
                {
                    CloseConfirmDialog();
                    origConfirm?.Invoke();
                },
                onCancel: () =>
                {
                    CloseConfirmDialog();
                    origCancel?.Invoke();
                }
            );

            ConfirmDialogViewModel = dialog;
            IsConfirmDialogOpen = true;
        }

        public void CloseConfirmDialog()
        {
            IsConfirmDialogOpen = false;
            ConfirmDialogViewModel = null;
        }

        public void OnLoginSuccess(UserDto user)
        {
            CurrentUser = user;
            IsAuthenticated = true;

            QueueViewModel = new TicketQueueViewModel(
                _apiClient,
                OnOpenTicketDetail,
                OnQuickRemote,
                OnQuickTracking,
                OnQuickInvoice
            );

            CurrentPage = "queue";
            UpdateNavigationState();
        }

        private void OnOpenTicketDetail(RepairRequestDto ticket)
        {
            DetailViewModel = new TicketDetailViewModel(
                _apiClient,
                _rustDeskService,
                ticket,
                OnBackToQueue,
                OnQuickTracking,
                OnQuickInvoice,
                ShowConfirmDialog
            );

            CurrentPage = "detail";
            UpdateNavigationState();
        }

        private void OnBackToQueue()
        {
            CurrentPage = "queue";
            UpdateNavigationState();
            _ = QueueViewModel?.LoadTicketsAsync();
        }

        private void OnQuickRemote(RepairRequestDto ticket)
        {
            if (!string.IsNullOrEmpty(ticket.RemoteSession?.RustdeskSessionId))
            {
                _rustDeskService.Connect(ticket.RemoteSession.RustdeskSessionId);
                if (ticket.RemoteSession.Id > 0)
                {
                    _ = _apiClient.StartRemoteSessionAsync(ticket.RemoteSession.Id);
                }
            }
            else
            {
                OnOpenTicketDetail(ticket);
            }
        }

        private void OnQuickTracking(RepairRequestDto ticket)
        {
            TrackingDialogViewModel = new WorkshopTrackingViewModel(
                _apiClient,
                ticket,
                () => IsTrackingDialogOpen = false,
                ShowConfirmDialog
            );
            IsTrackingDialogOpen = true;
        }

        private void OnQuickInvoice(RepairRequestDto ticket)
        {
            InvoiceDialogViewModel = new InvoiceManagerViewModel(
                _apiClient,
                ticket,
                () => IsInvoiceDialogOpen = false,
                ShowConfirmDialog
            );
            IsInvoiceDialogOpen = true;
        }

        private void UpdateNavigationState()
        {
            OnPropertyChanged(nameof(IsAuthPage));
            OnPropertyChanged(nameof(IsQueuePage));
            OnPropertyChanged(nameof(IsDetailPage));
            OnPropertyChanged(nameof(TechnicianName));
            OnPropertyChanged(nameof(TechnicianEmail));
            OnPropertyChanged(nameof(TechnicianRole));
        }

        [RelayCommand]
        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            var newTheme = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
            _themeService.SaveTheme(IsDarkMode ? "Dark" : "Light");

            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = newTheme;
            }
        }

        [RelayCommand]
        public void Logout()
        {
            ShowConfirmDialog(new ConfirmDialogViewModel(
                title: "Konfirmasi Keluar Akun",
                message: "Apakah Anda yakin ingin keluar dari Workbench Teknisi PT JTS di perangkat ini? Sesi koneksi remote aktif atau perubahan yang belum disimpan dapat terputus.",
                onConfirm: ExecuteLogout,
                onCancel: () => { },
                detailNote: $"Akun Teknisi Aktif: {TechnicianName} ({TechnicianEmail})",
                confirmText: "Ya, Keluar Akun",
                cancelText: "Batal",
                dialogType: "danger",
                badgeText: "LOGOUT TEKNISI"
            ));
        }

        public void ExecuteLogout()
        {
            _apiClient.ClearToken();
            CurrentUser = null;
            IsAuthenticated = false;
            DetailViewModel = null;
            CurrentPage = "auth";
            UpdateNavigationState();
        }

        [RelayCommand]
        public async Task RefreshAllAsync()
        {
            if (QueueViewModel != null)
            {
                await QueueViewModel.LoadTicketsAsync();
            }

            try
            {
                var (success, msg, latency) = await _apiClient.PingAsync();
                IsServerOnline = success;
                ServerStatusText = success ? $"Online ({latency}ms)" : "Offline";
            }
            catch
            {
                IsServerOnline = false;
                ServerStatusText = "Offline";
            }
        }
    }
}
