using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public partial class AuthViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly DeviceIdentifierService _deviceService;
        private readonly Action<UserDto, DeviceDto?> _onAuthSuccess;

        [ObservableProperty]
        private string _hardwareId = string.Empty;

        [ObservableProperty]
        private string _detectedModel = string.Empty;

        [ObservableProperty]
        private string _detectedSerial = string.Empty;

        [ObservableProperty]
        private string _serverUrl = string.Empty;

        [ObservableProperty]
        private bool _isServerConfigExpanded = false;

        [ObservableProperty]
        private string? _serverStatusMessage;

        [ObservableProperty]
        private bool _isServerOnline = false;

        [ObservableProperty]
        private bool _isTestingConnection = false;

        [ObservableProperty]
        private bool _isRegisterMode = false;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _address = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private string? _infoMessage;

        public AuthViewModel(ApiClient apiClient, DeviceIdentifierService deviceService, Action<UserDto, DeviceDto?> onAuthSuccess)
        {
            _apiClient = apiClient;
            _deviceService = deviceService;
            _onAuthSuccess = onAuthSuccess;

            // Auto-read physical BIOS/Hardware ID & model of this specific machine
            HardwareId = _deviceService.GetHardwareId();
            DetectedModel = _deviceService.GetDeviceModel();
            DetectedSerial = _deviceService.GetSerialNumber();
            ServerUrl = _apiClient.BaseUrl;
            InfoMessage = $"Identitas Motherboard/BIOS Komputer Ini: {HardwareId}";

            // Check server status silently on startup
            _ = CheckServerConnectionSilentlyAsync();
        }

        private async Task CheckServerConnectionSilentlyAsync()
        {
            try
            {
                var (success, msg, latency) = await _apiClient.PingAsync();
                IsServerOnline = success;
                ServerStatusMessage = success 
                    ? $"✓ Server Terhubung ({latency}ms)" 
                    : $"⚠️ Server Belum Terhubung: {msg}";
            }
            catch
            {
                IsServerOnline = false;
                ServerStatusMessage = "⚠️ Server Belum Terhubung";
            }
        }

        [RelayCommand]
        private void ToggleServerConfig()
        {
            IsServerConfigExpanded = !IsServerConfigExpanded;
        }

        [RelayCommand]
        private async Task TestConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                ServerStatusMessage = "⚠️ Harap masukkan alamat Server URL.";
                IsServerOnline = false;
                return;
            }

            IsTestingConnection = true;
            ServerStatusMessage = "Sedang memeriksa koneksi...";

            try
            {
                var (success, msg, latency) = await _apiClient.PingAsync(ServerUrl);
                IsServerOnline = success;
                ServerStatusMessage = success 
                    ? $"✓ Berhasil terhubung ke server ({latency}ms)!" 
                    : $"✗ Gagal: {msg}";
            }
            catch (Exception ex)
            {
                IsServerOnline = false;
                ServerStatusMessage = $"✗ Gagal: {ex.Message}";
            }
            finally
            {
                IsTestingConnection = false;
            }
        }

        [RelayCommand]
        private async Task SaveServerUrlAsync()
        {
            if (string.IsNullOrWhiteSpace(ServerUrl)) return;

            _apiClient.UpdateBaseUrl(ServerUrl);
            ServerUrl = _apiClient.BaseUrl;
            await TestConnectionAsync();
        }

        [RelayCommand]
        private void ToggleMode()
        {
            IsRegisterMode = !IsRegisterMode;
            ErrorMessage = null;
        }

        [RelayCommand]
        private void FillDemoCustomer()
        {
            IsRegisterMode = false;
            Email = "customer@example.com";
            Password = "password";
            ErrorMessage = null;
        }

        [RelayCommand]
        private async Task SubmitAsync()
        {
            IsLoading = true;
            ErrorMessage = null;

            try
            {
                if (IsRegisterMode)
                {
                    if (string.IsNullOrWhiteSpace(Name) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                    {
                        ErrorMessage = "Nama, Email, dan Password wajib diisi.";
                        IsLoading = false;
                        return;
                    }

                    var res = await _apiClient.RegisterAsync(
                        Name.Trim(),
                        Email.Trim(),
                        Password,
                        Phone?.Trim(),
                        Address?.Trim(),
                        HardwareId,
                        DetectedModel,
                        DetectedSerial
                    );

                    var device = res.ActiveDevice ?? res.LinkedDevice;
                    _onAuthSuccess(res.User!, device);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                    {
                        ErrorMessage = "Email dan Password wajib diisi.";
                        IsLoading = false;
                        return;
                    }

                    var res = await _apiClient.LoginAsync(Email.Trim(), Password, HardwareId, DetectedModel, DetectedSerial);

                    // Strictly match with THIS physical PC's Hardware ID only
                    DeviceDto? device = res.ActiveDevice ?? res.LinkedDevice;

                    if (device == null && res.User?.Devices != null)
                    {
                        device = res.User.Devices.Find(d => d.HardwareId.Equals(HardwareId, StringComparison.OrdinalIgnoreCase));
                        // Never fall back to an arbitrary device index 0!
                    }

                    _onAuthSuccess(res.User!, device);
                }
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
    }
}
