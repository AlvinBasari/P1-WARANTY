using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public partial class LocationViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly Func<DeviceDto?> _getDevice;
        private readonly Action<DeviceDto> _onLocationUpdated;
        private readonly Func<UserDto?> _getUser;
        private readonly Action<UserDto>? _onUserUpdated;

        // ─────────────────────────────────────────────────────────────
        // 1. User Profile & WhatsApp Fields
        // ─────────────────────────────────────────────────────────────
        [ObservableProperty]
        private string _userName = string.Empty;

        [ObservableProperty]
        private string _userEmail = string.Empty;

        [ObservableProperty]
        private string _userPhone = string.Empty;

        [ObservableProperty]
        private string _userWhatsapp = string.Empty;

        [ObservableProperty]
        private string? _userAvatarUrl;

        [ObservableProperty]
        private string? _localAvatarFilePath;

        [ObservableProperty]
        private Bitmap? _avatarBitmap;

        [ObservableProperty]
        private string _userInitial = "U";

        [ObservableProperty]
        private bool _isAvatarUploading;

        // ─────────────────────────────────────────────────────────────
        // 2. Structured Address & Location Fields
        // ─────────────────────────────────────────────────────────────
        [ObservableProperty]
        private bool _isSaved;

        [ObservableProperty]
        private bool _isEditMode = true;

        [ObservableProperty]
        private bool _isDetectingGps;

        [ObservableProperty]
        private string? _gpsStatusMessage;

        [ObservableProperty]
        private string _locationType = "Rumah";

        [ObservableProperty]
        private string _street = string.Empty;

        [ObservableProperty]
        private string _rt = string.Empty;

        [ObservableProperty]
        private string _rw = string.Empty;

        [ObservableProperty]
        private string _kelurahan = string.Empty;

        [ObservableProperty]
        private string _kecamatan = string.Empty;

        [ObservableProperty]
        private string _city = "Jakarta Selatan";

        [ObservableProperty]
        private string _province = "DKI Jakarta";

        [ObservableProperty]
        private string _landmark = string.Empty;

        // GPS Coordinates
        [ObservableProperty]
        private double _latitude = -6.229728;

        [ObservableProperty]
        private double _longitude = 106.855845;

        // Read-only Display Properties for Saved State
        [ObservableProperty]
        private string _savedLocationType = "Lokasi Unit";

        [ObservableProperty]
        private string _savedFullAddress = "Belum diatur";

        [ObservableProperty]
        private string _savedCoordinates = "Belum terdeteksi";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _successMessage;

        [ObservableProperty]
        private string? _errorMessage;

        public LocationViewModel(
            ApiClient apiClient,
            Func<DeviceDto?> getDevice,
            Action<DeviceDto> onLocationUpdated,
            Func<UserDto?>? getUser = null,
            Action<UserDto>? onUserUpdated = null)
        {
            _apiClient = apiClient;
            _getDevice = getDevice;
            _onLocationUpdated = onLocationUpdated;
            _getUser = getUser ?? (() => null);
            _onUserUpdated = onUserUpdated;

            LoadUserProfile();
            LoadDeviceLocation();
        }

        public void LoadUserProfile(UserDto? user = null)
        {
            var u = user ?? _getUser();
            if (u != null)
            {
                UserName = u.Name ?? "";
                UserEmail = u.Email ?? "";
                UserPhone = u.Phone ?? "";
                UserWhatsapp = !string.IsNullOrWhiteSpace(u.WhatsappNumber) ? u.WhatsappNumber : (u.Phone ?? "");
                UserAvatarUrl = u.AvatarUrl;

                UserInitial = !string.IsNullOrWhiteSpace(UserName)
                    ? UserName.Substring(0, 1).ToUpperInvariant()
                    : "U";

                if (!string.IsNullOrWhiteSpace(u.AvatarUrl))
                {
                    _ = LoadAvatarFromUrlOrPathAsync(u.AvatarUrl);
                }
            }
        }

        partial void OnUserNameChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                UserInitial = "U";
            }
            else
            {
                var trimmed = value.Trim();
                UserInitial = trimmed.Length > 0 ? trimmed.Substring(0, 1).ToUpperInvariant() : "U";
            }
        }

        public void LoadDeviceLocation()
        {
            var dev = _getDevice();
            if (dev?.LocationLat != null && dev.LocationLng != null && !string.IsNullOrWhiteSpace(dev.LocationLabel))
            {
                Latitude = dev.LocationLat.Value;
                Longitude = dev.LocationLng.Value;
                SavedCoordinates = $"{Latitude:F6}, {Longitude:F6}";

                string label = dev.LocationLabel;
                if (label.Contains(" - "))
                {
                    var parts = label.Split(new[] { " - " }, 2, StringSplitOptions.None);
                    SavedLocationType = parts[0];
                    LocationType = parts[0];
                    SavedFullAddress = parts[1];
                    Street = parts[1];
                }
                else
                {
                    SavedFullAddress = label;
                    Street = label;
                }

                IsSaved = true;
                IsEditMode = false;
            }
            else
            {
                IsSaved = false;
                IsEditMode = true;
                _ = AutoDetectLocationAsync();
            }
        }

        [RelayCommand]
        public async Task PickAvatarImageAsync()
        {
            ErrorMessage = null;

            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
            {
                try
                {
                    var storageProvider = desktop.MainWindow.StorageProvider;
                    var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
                    {
                        Title = "Pilih Foto Profil (JPG / PNG / WebP)",
                        AllowMultiple = false,
                        FileTypeFilter = new[]
                        {
                            new Avalonia.Platform.Storage.FilePickerFileType("Berkas Gambar")
                            {
                                Patterns = new[] { "*.jpg", "*.jpeg", "*.png", "*.webp" }
                            }
                        }
                    });

                    if (files.Count > 0)
                    {
                        var localPath = files[0].Path.LocalPath;
                        LocalAvatarFilePath = localPath;

                        // Load preview immediately
                        try
                        {
                            AvatarBitmap = new Bitmap(localPath);
                            SuccessMessage = "✓ Foto profil baru dipilih. Klik 'Simpan Perubahan Akun & Lokasi' untuk menerapkan.";
                        }
                        catch (Exception ex)
                        {
                            ErrorMessage = $"Gagal memuat gambar: {ex.Message}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Gagal membuka pemilih berkas: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void RemoveAvatar()
        {
            LocalAvatarFilePath = null;
            UserAvatarUrl = null;
            AvatarBitmap = null;
            SuccessMessage = "Foto profil dihapus. Klik 'Simpan Perubahan' untuk menerapkan.";
        }

        [RelayCommand]
        public async Task AutoDetectLocationAsync()
        {
            IsDetectingGps = true;
            GpsStatusMessage = "Mendeteksi koordinat GPS jaringan...";

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(4) };
                var json = await http.GetStringAsync("http://ip-api.com/json");
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp) && statusProp.GetString() == "success")
                {
                    if (root.TryGetProperty("lat", out var latProp))
                        Latitude = latProp.GetDouble();
                    if (root.TryGetProperty("lon", out var lonProp))
                        Longitude = lonProp.GetDouble();

                    if (root.TryGetProperty("city", out var cityProp))
                        City = cityProp.GetString() ?? City;
                    if (root.TryGetProperty("regionName", out var regionProp))
                        Province = regionProp.GetString() ?? Province;

                    GpsStatusMessage = $"✓ GPS Terdeteksi: {Latitude:F4}, {Longitude:F4} ({City})";
                }
                else
                {
                    GpsStatusMessage = "Lokasi default Jakarta digunakan (GPS presisi tetap aktif)";
                }
            }
            catch
            {
                GpsStatusMessage = "Menggunakan titik koordinat patokan default.";
            }
            finally
            {
                IsDetectingGps = false;
            }
        }

        [RelayCommand]
        public void EnterEditMode()
        {
            IsEditMode = true;
            SuccessMessage = null;
            ErrorMessage = null;
        }

        [RelayCommand]
        public async Task SaveProfileAndLocationAsync()
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                ErrorMessage = "Nama lengkap tidak boleh kosong.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Street) || Street.Length < 4)
            {
                ErrorMessage = "Harap masukkan alamat jalan / nomor rumah yang lengkap (minimal 4 karakter).";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            try
            {
                // 1. Upload Avatar if new local image was selected
                string? finalAvatarUrl = UserAvatarUrl;
                if (!string.IsNullOrWhiteSpace(LocalAvatarFilePath) && File.Exists(LocalAvatarFilePath))
                {
                    try
                    {
                        finalAvatarUrl = await _apiClient.UploadAvatarAsync(LocalAvatarFilePath);
                        UserAvatarUrl = finalAvatarUrl;
                    }
                    catch
                    {
                        // Fallback keep existing if upload fails
                    }
                }

                // 2. Build structured full address
                var addressParts = new List<string> { Street.Trim() };
                if (!string.IsNullOrWhiteSpace(Rt) || !string.IsNullOrWhiteSpace(Rw))
                    addressParts.Add($"RT {Rt.Trim()}/RW {Rw.Trim()}");
                if (!string.IsNullOrWhiteSpace(Kelurahan))
                    addressParts.Add($"Kel. {Kelurahan.Trim()}");
                if (!string.IsNullOrWhiteSpace(Kecamatan))
                    addressParts.Add($"Kec. {Kecamatan.Trim()}");
                if (!string.IsNullOrWhiteSpace(City))
                    addressParts.Add(City.Trim());
                if (!string.IsNullOrWhiteSpace(Province))
                    addressParts.Add(Province.Trim());
                if (!string.IsNullOrWhiteSpace(Landmark))
                    addressParts.Add($"(Patokan: {Landmark.Trim()})");

                string fullAddress = string.Join(", ", addressParts);
                string locationLabel = $"{LocationType} - {fullAddress}";

                // 3. Update User Profile in Backend
                var updatedUser = await _apiClient.UpdateProfileAsync(
                    UserName.Trim(),
                    UserEmail.Trim(),
                    UserPhone.Trim(),
                    UserWhatsapp.Trim(),
                    fullAddress,
                    finalAvatarUrl
                );

                if (updatedUser != null)
                {
                    _onUserUpdated?.Invoke(updatedUser);
                    UserInitial = !string.IsNullOrWhiteSpace(updatedUser.Name)
                        ? updatedUser.Name.Substring(0, 1).ToUpperInvariant()
                        : "U";
                }

                // 4. Update Device Calibrated Location
                var device = _getDevice();
                if (device != null)
                {
                    var updatedDevice = await _apiClient.CalibrateLocationAsync(
                        device.Id,
                        Latitude,
                        Longitude,
                        locationLabel
                    );

                    _onLocationUpdated(updatedDevice);
                }

                SavedLocationType = LocationType;
                SavedFullAddress = fullAddress;
                SavedCoordinates = $"{Latitude:F6}, {Longitude:F6}";

                IsSaved = true;
                IsEditMode = false;
                LocalAvatarFilePath = null;
                SuccessMessage = "✓ Data akun, No. WhatsApp, dan titik lokasi berhasil diperbarui!";
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

        private async Task LoadAvatarFromUrlOrPathAsync(string urlOrPath)
        {
            try
            {
                if (File.Exists(urlOrPath))
                {
                    AvatarBitmap = new Bitmap(urlOrPath);
                }
                else if (urlOrPath.StartsWith("http://") || urlOrPath.StartsWith("https://"))
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                    var bytes = await http.GetByteArrayAsync(urlOrPath);
                    using var ms = new MemoryStream(bytes);
                    AvatarBitmap = new Bitmap(ms);
                }
            }
            catch
            {
                // Fallback icon will be shown
            }
        }
    }
}
