using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly DeviceIdentifierService _deviceService;
        private readonly RustDeskService _rustdeskService;
        private readonly ThemeSettingsService _themeSettingsService;

        [ObservableProperty]
        private ViewModelBase? _currentView;

        [ObservableProperty]
        private UserDto? _currentUser;

        [ObservableProperty]
        private DeviceDto? _currentDevice;

        [ObservableProperty]
        private string _activeTab = "Warranty";

        [ObservableProperty]
        private bool _isAuthenticated = false;

        [ObservableProperty]
        private bool _isCheckingUpdates = false;

        [ObservableProperty]
        private bool _isOfflineMode = false;

        [ObservableProperty]
        private bool _isDarkMode = true;

        [ObservableProperty]
        private string _themeButtonText = "Mode Terang";

        [ObservableProperty]
        private string _themeButtonIcon = "IconSun";

        [ObservableProperty]
        private string? _updateMessage;

        [ObservableProperty]
        private string _refreshButtonText = "Segarkan";

        [ObservableProperty]
        private string _refreshButtonIcon = "IconRefresh";

        [ObservableProperty]
        private bool _isRefreshSuccess = false;

        [ObservableProperty]
        private Avalonia.Media.Imaging.Bitmap? _userAvatarBitmap;

        [ObservableProperty]
        private string _userInitial = "U";

        public AuthViewModel AuthVm { get; }
        public WarrantyViewModel WarrantyVm { get; }
        public DiagnosticViewModel DiagnosticVm { get; }
        public ComponentDetailViewModel ComponentDetailVm { get; }
        public ClaimViewModel ClaimVm { get; }
        public TrackingViewModel TrackingVm { get; }
        public LocationViewModel LocationVm { get; }

        public MainViewModel(ApiClient apiClient, DeviceIdentifierService deviceService, RustDeskService rustdeskService)
        {
            _apiClient = apiClient;
            _deviceService = deviceService;
            _rustdeskService = rustdeskService;
            _themeSettingsService = new ThemeSettingsService();

            AuthVm = new AuthViewModel(_apiClient, _deviceService, OnAuthenticated);
            WarrantyVm = new WarrantyViewModel(_apiClient, null, NavigateToTab, OnDeviceActivated);
            DiagnosticVm = new DiagnosticViewModel(_apiClient, _deviceService, NavigateToTab);
            ComponentDetailVm = new ComponentDetailViewModel(NavigateToTab);
            ClaimVm = new ClaimViewModel(_apiClient, _rustdeskService, () => CurrentDevice, OnClaimSubmitted);
            TrackingVm = new TrackingViewModel(_apiClient);
            LocationVm = new LocationViewModel(_apiClient, () => CurrentDevice, OnLocationUpdated, () => CurrentUser, OnUserUpdated);

            // Load saved theme preference
            var savedTheme = _themeSettingsService.GetSavedTheme();
            IsDarkMode = !savedTheme.Equals("Light", StringComparison.OrdinalIgnoreCase);
            ApplyTheme(IsDarkMode, save: false);

            // Start on Auth if not logged in
            CurrentView = AuthVm;
        }

        [RelayCommand]
        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
            ApplyTheme(IsDarkMode, save: true);
        }

        private void ApplyTheme(bool isDark, bool save)
        {
            if (Avalonia.Application.Current != null)
            {
                Avalonia.Application.Current.RequestedThemeVariant = isDark
                    ? Avalonia.Styling.ThemeVariant.Dark
                    : Avalonia.Styling.ThemeVariant.Light;
            }

            ThemeButtonText = isDark ? "Mode Terang" : "Mode Gelap";
            ThemeButtonIcon = isDark ? "IconSun" : "IconMoon";

            if (save)
            {
                _themeSettingsService.SaveTheme(isDark ? "Dark" : "Light");
            }

            OnPropertyChanged(nameof(ActiveTab));
        }

        private void OnAuthenticated(UserDto user, DeviceDto? device)
        {
            CurrentUser = user;
            CurrentDevice = device;
            IsAuthenticated = true;
            IsOfflineMode = false;

            UserInitial = !string.IsNullOrWhiteSpace(user.Name) ? user.Name.Substring(0, 1).ToUpperInvariant() : "U";
            _ = LoadHeaderAvatarAsync(user.AvatarUrl);

            WarrantyVm.UpdateDevice(device);
            LocationVm.LoadUserProfile(user);
            LocationVm.LoadDeviceLocation();

            // Auto-check if device has active tracking (select latest workshop request)
            if (device?.RepairRequests != null)
            {
                RepairRequestDto? latestWorkshop = null;
                foreach (var req in device.RepairRequests)
                {
                    if (req.NeedsOfficeRepair || req.Type == "on_site")
                    {
                        if (latestWorkshop == null || req.Id > latestWorkshop.Id)
                        {
                            latestWorkshop = req;
                        }
                    }
                }

                if (latestWorkshop != null)
                {
                    _ = TrackingVm.LoadTrackingForRequestAsync(latestWorkshop.Id);
                }
            }

            NavigateToTab("Warranty");
        }

        private void OnUserUpdated(UserDto updatedUser)
        {
            CurrentUser = updatedUser;
            UserInitial = !string.IsNullOrWhiteSpace(updatedUser.Name) ? updatedUser.Name.Substring(0, 1).ToUpperInvariant() : "U";
            _ = LoadHeaderAvatarAsync(updatedUser.AvatarUrl);
        }

        private async Task LoadHeaderAvatarAsync(string? avatarUrl)
        {
            if (string.IsNullOrWhiteSpace(avatarUrl))
            {
                UserAvatarBitmap = null;
                return;
            }

            try
            {
                if (System.IO.File.Exists(avatarUrl))
                {
                    UserAvatarBitmap = new Avalonia.Media.Imaging.Bitmap(avatarUrl);
                }
                else if (avatarUrl.StartsWith("http://") || avatarUrl.StartsWith("https://"))
                {
                    using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                    var bytes = await http.GetByteArrayAsync(avatarUrl);
                    using var ms = new System.IO.MemoryStream(bytes);
                    UserAvatarBitmap = new Avalonia.Media.Imaging.Bitmap(ms);
                }
            }
            catch
            {
                UserAvatarBitmap = null;
            }
        }

        private void OnDeviceActivated(DeviceDto activatedDevice)
        {
            CurrentDevice = activatedDevice;
            if (CurrentUser != null)
            {
                CurrentUser.Devices ??= new();
                CurrentUser.Devices.Add(activatedDevice);
            }
        }

        private void OnClaimSubmitted(RepairRequestDto request)
        {
            if (request.NeedsOfficeRepair || request.Type == "on_site")
            {
                _ = TrackingVm.LoadTrackingForRequestAsync(request.Id);
            }
        }

        private void OnLocationUpdated(DeviceDto updatedDevice)
        {
            CurrentDevice = updatedDevice;
            WarrantyVm.UpdateDevice(updatedDevice);
        }

        [RelayCommand]
        public async Task CheckForUpdatesAsync()
        {
            if (IsCheckingUpdates) return;

            IsCheckingUpdates = true;
            IsRefreshSuccess = false;
            RefreshButtonText = "Menyegarkan...";
            RefreshButtonIcon = "IconRefresh";

            await Task.Delay(800);

            try
            {
                // Refresh device warranty status from backend
                var hwId = CurrentDevice?.HardwareId ?? _deviceService.GetHardwareId();
                if (!string.IsNullOrEmpty(hwId))
                {
                    var lookup = await _apiClient.LookupDeviceAsync(hwId);
                    if (lookup.Found && lookup.Device != null)
                    {
                        CurrentDevice = lookup.Device;
                        WarrantyVm.UpdateDevice(lookup.Device);
                    }
                }

                IsOfflineMode = false;
                WarrantyVm.LoadSystemMetrics();
                IsRefreshSuccess = true;
                RefreshButtonText = "Status Terkini";
                RefreshButtonIcon = "IconCheck";
            }
            catch
            {
                IsOfflineMode = true;
                WarrantyVm.LoadSystemMetrics();
                IsRefreshSuccess = false;
                RefreshButtonText = "Mode Offline";
                RefreshButtonIcon = "IconAlert";
            }
            finally
            {
                IsCheckingUpdates = false;
            }

            await Task.Delay(2500);
            RefreshButtonText = "Segarkan";
            RefreshButtonIcon = "IconRefresh";
            IsRefreshSuccess = false;
        }

        [RelayCommand]
        public void NavigateToTab(string tabName)
        {
            if (tabName.StartsWith("ComponentDetail", StringComparison.OrdinalIgnoreCase))
            {
                ActiveTab = "ComponentDetail";
                if (tabName.Contains("_CPU", StringComparison.OrdinalIgnoreCase))
                    ComponentDetailVm.SelectCategory("CPU");
                else if (tabName.Contains("_RAM", StringComparison.OrdinalIgnoreCase))
                    ComponentDetailVm.SelectCategory("RAM");
                else if (tabName.Contains("_Storage", StringComparison.OrdinalIgnoreCase))
                    ComponentDetailVm.SelectCategory("Storage");
                else if (tabName.Contains("_Motherboard", StringComparison.OrdinalIgnoreCase))
                    ComponentDetailVm.SelectCategory("Motherboard");

                ComponentDetailVm.RefreshAll();
                CurrentView = ComponentDetailVm;
                return;
            }

            ActiveTab = tabName;
            CurrentView = tabName switch
            {
                "Auth" => AuthVm,
                "Warranty" => WarrantyVm,
                "Diagnostic" => DiagnosticVm,
                "ComponentDetail" => ComponentDetailVm,
                "Claim" => ClaimVm,
                "Tracking" => TrackingVm,
                "Location" => LocationVm,
                _ => WarrantyVm
            };
        }

        [RelayCommand]
        public void Logout()
        {
            _apiClient.ClearToken();
            CurrentUser = null;
            CurrentDevice = null;
            IsAuthenticated = false;
            CurrentView = AuthVm;
        }
    }
}
