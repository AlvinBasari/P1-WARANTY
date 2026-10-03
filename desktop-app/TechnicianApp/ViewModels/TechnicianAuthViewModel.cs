using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace TechnicianApp.ViewModels
{
    public partial class TechnicianAuthViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly Action<UserDto> _onLoginSuccess;

        [ObservableProperty]
        private string _email = "technician@warranty.com";

        [ObservableProperty]
        private string _password = "password";

        [ObservableProperty]
        private string _serverUrl = "http://127.0.0.1:8000/api";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _errorMessage;

        [ObservableProperty]
        private bool _isConnected = true;

        [ObservableProperty]
        private string _connectionStatusText = "Server Siap (127.0.0.1:8000)";

        public TechnicianAuthViewModel(ApiClient apiClient, Action<UserDto> onLoginSuccess)
        {
            _apiClient = apiClient;
            _onLoginSuccess = onLoginSuccess;
            ServerUrl = _apiClient.BaseUrl;
            _ = CheckConnectionAsync();
        }

        [RelayCommand]
        public async Task CheckConnectionAsync()
        {
            try
            {
                var (success, msg, latency) = await _apiClient.PingAsync(ServerUrl);
                IsConnected = success;
                ConnectionStatusText = success ? $"Terhubung ({latency}ms)" : $"Offline: {msg}";
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatusText = $"Gagal koneksi: {ex.Message}";
            }
        }

        [RelayCommand]
        public void FillDemoCredentials()
        {
            Email = "technician@warranty.com";
            Password = "password";
            ErrorMessage = null;
        }

        [RelayCommand]
        public async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Harap isi email dan kata sandi teknisi.";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            try
            {
                if (_apiClient.BaseUrl != ServerUrl)
                {
                    _apiClient.UpdateBaseUrl(ServerUrl);
                }

                var authResponse = await _apiClient.LoginAsync(Email.Trim(), Password);
                if (authResponse.User != null)
                {
                    _onLoginSuccess(authResponse.User);
                }
                else
                {
                    ErrorMessage = "Gagal memuat profil pengguna.";
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
