using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CustomerApp.Services;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public partial class WarrantyViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly DeviceIdentifierService _deviceService = new();
        private readonly Action<string> _navigate;
        private readonly Action<DeviceDto>? _onDeviceUpdated;
        private readonly SystemMetricsService _metricsService = new();

        [ObservableProperty]
        private DeviceDto? _device;

        [ObservableProperty]
        private WarrantyDto? _warranty;

        [ObservableProperty]
        private bool _isDeviceRegistered;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(WarrantySweepAngle))]
        private bool _isWarrantyActive;

        public double WarrantySweepAngle => IsWarrantyActive ? 360.0 : 0.0;

        [ObservableProperty]
        private string _statusLabel = "UNIT BELUM TERDAFTAR";

        [ObservableProperty]
        private string _warrantyRemainingText = "Memuat...";

        [ObservableProperty]
        private string _locationSummary = "Belum diatur";

        [ObservableProperty]
        private string _conditionTitle = "Kondisi Unit";

        [ObservableProperty]
        private string _conditionStatus = "Unit Prima (0x Servis)";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ConditionSweepAngle))]
        private int _conditionPercent = 100;

        [ObservableProperty]
        private string _conditionDisplay = "100%";

        public double ConditionSweepAngle => Math.Clamp(ConditionPercent * 3.6, 1.0, 360.0);

        [ObservableProperty]
        private bool _hasActiveRepair = false;

        [ObservableProperty]
        private string _storageDisplay = "56.3 GB / 468 GB";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StorageSweepAngle))]
        private int _storagePercent = 12;

        public double StorageSweepAngle => Math.Clamp(StoragePercent * 3.6, 1.0, 360.0);

        [ObservableProperty]
        private string _powerLabel = "Kondisi Daya";

        [ObservableProperty]
        private string _powerDisplay = "AC";

        [ObservableProperty]
        private int _powerPercent = 100;

        public double PowerSweepAngle => Math.Clamp(PowerPercent * 3.6, 1.0, 360.0);

        [ObservableProperty]
        private string _powerStatus = "Daya Listrik AC Stabil";

        [ObservableProperty]
        private string _memoryDisplay = "3.4 GB / 7.6 GB";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MemorySweepAngle))]
        private int _memoryPercent = 44;

        public double MemorySweepAngle => Math.Clamp(MemoryPercent * 3.6, 1.0, 360.0);

        [ObservableProperty]
        private string _biosVersion = "M16KT37A";

        [ObservableProperty]
        private string _osDescription = "Linux / Windows 64-bit";

        [ObservableProperty]
        private string _processorName = "Intel Core Processor";

        [ObservableProperty]
        private int _processorCores = 4;

        [ObservableProperty]
        private string _supportContact = "0812-9900-1122 (Senin - Sabtu 08:00 - 17:00 WIB)";

        [ObservableProperty]
        private string _networkLatencyText = "Mengukur...";

        [ObservableProperty]
        private string _localHardwareId = string.Empty;

        [ObservableProperty]
        private string _localModel = string.Empty;

        [ObservableProperty]
        private string _localSerial = string.Empty;

        [ObservableProperty]
        private string _claimTokenInput = string.Empty;

        [ObservableProperty]
        private bool _isActivating;

        [ObservableProperty]
        private string? _activationError;

        [ObservableProperty]
        private string? _copyToast;

        public WarrantyViewModel(ApiClient apiClient, DeviceDto? device, Action<string> navigate, Action<DeviceDto>? onDeviceUpdated = null)
        {
            _apiClient = apiClient;
            _navigate = navigate;
            _onDeviceUpdated = onDeviceUpdated;

            LocalHardwareId = _deviceService.GetHardwareId();
            LocalModel = _deviceService.GetDeviceModel();
            LocalSerial = _deviceService.GetSerialNumber();

            LoadSystemMetrics();
            UpdateDevice(device);
        }

        public void LoadSystemMetrics()
        {
            try
            {
                var metrics = _metricsService.GetSystemMetrics();
                StorageDisplay = metrics.StorageDisplay;
                StoragePercent = metrics.StoragePercentUsed;

                MemoryDisplay = metrics.MemoryDisplay;
                MemoryPercent = metrics.MemoryPercentUsed;

                PowerLabel = metrics.PowerLabel;
                PowerDisplay = metrics.PowerDisplay;
                PowerPercent = metrics.PowerPercent;
                PowerStatus = metrics.PowerStatus;

                BiosVersion = metrics.BiosVersion;
                OsDescription = metrics.OsDescription;
                ProcessorName = metrics.ProcessorName;
                ProcessorCores = metrics.ProcessorCores;

                _ = LoadNetworkLatencyAsync();
            }
            catch { }
        }

        private async Task LoadNetworkLatencyAsync()
        {
            try
            {
                var quality = await _metricsService.GetNetworkQualityAsync();
                NetworkLatencyText = $"Ping: {quality.PingMs} ms • {quality.StatusText}";
            }
            catch
            {
                NetworkLatencyText = "Koneksi Stabil";
            }
        }

        public void UpdateDevice(DeviceDto? device)
        {
            Device = device;
            Warranty = device?.Warranty;

            int repairs = 0;
            HasActiveRepair = false;

            if (device?.RepairRequests != null)
            {
                repairs = device.RepairRequests.Count;
                foreach (var req in device.RepairRequests)
                {
                    if (req.Status != "completed" && req.Status != "cancelled")
                    {
                        HasActiveRepair = true;
                        break;
                    }
                }
            }

            int deduction = repairs * 5;
            ConditionPercent = Math.Max(50, 100 - deduction);
            ConditionDisplay = $"{ConditionPercent}%";

            if (HasActiveRepair)
            {
                ConditionStatus = $"{repairs}x Servis (Dalam Proses)";
            }
            else if (repairs == 0)
            {
                ConditionStatus = "Unit Prima (0x Servis)";
            }
            else
            {
                ConditionStatus = $"{repairs}x Riwayat Servis";
            }

            if (device != null && Warranty != null)
            {
                IsDeviceRegistered = true;
                IsWarrantyActive = Warranty.Status == "active";
                StatusLabel = IsWarrantyActive ? "GARANSI RESMI AKTIF" : "GARANSI BERAKHIR";

                if (!string.IsNullOrEmpty(Warranty.WarrantyEnd) && DateTime.TryParse(Warranty.WarrantyEnd, out var endDate))
                {
                    var daysRemaining = (endDate - DateTime.Today).Days;
                    if (daysRemaining > 30)
                    {
                        var months = (int)Math.Round(daysRemaining / 30.0);
                        WarrantyRemainingText = $"{months} Bulan Tersisa";
                    }
                    else if (daysRemaining > 0)
                    {
                        WarrantyRemainingText = $"{daysRemaining} Hari Tersisa";
                    }
                    else
                    {
                        WarrantyRemainingText = "Masa Garansi Berakhir";
                    }
                }
            }
            else
            {
                IsDeviceRegistered = false;
                IsWarrantyActive = false;
                StatusLabel = "UNIT BELUM TERDAFTAR";
                WarrantyRemainingText = "Belum Terdaftar di PT JTS";
            }

            if (Device?.LocationLat != null && Device?.LocationLng != null)
            {
                LocationSummary = $"{Device.LocationLabel ?? "Tersimpan"} ({Device.LocationLat:F4}, {Device.LocationLng:F4})";
            }
            else
            {
                LocationSummary = "Belum diatur (Perlu kalibrasi untuk servis on-site)";
            }
        }

        [RelayCommand]
        public void OpenRepairHistory()
        {
            _navigate("Tracking");
        }

        [RelayCommand]
        public async Task ActivateWithTokenAsync()
        {
            if (string.IsNullOrWhiteSpace(ClaimTokenInput) || ClaimTokenInput.Length < 3)
            {
                ActivationError = "Harap masukkan Token QR atau Serial Number dari stiker fisik unit.";
                return;
            }

            IsActivating = true;
            ActivationError = null;

            try
            {
                var activatedDevice = await _apiClient.ClaimDeviceByTokenAsync(ClaimTokenInput.Trim(), LocalHardwareId, LocalModel);
                UpdateDevice(activatedDevice);
                _onDeviceUpdated?.Invoke(activatedDevice);
                ClaimTokenInput = string.Empty;
                _ = ShowToastAsync("Garansi resmi PT JTS berhasil diaktivasi!");
            }
            catch (Exception ex)
            {
                ActivationError = ex.Message;
            }
            finally
            {
                IsActivating = false;
            }
        }

        [RelayCommand]
        private void CopyCsSummary()
        {
            var model = Device?.Model ?? LocalModel;
            var serial = Device?.SerialNumber ?? LocalSerial;
            var hwId = Device?.HardwareId ?? LocalHardwareId;
            var warrantyStatus = IsWarrantyActive ? $"Aktif s/d {Warranty?.WarrantyEnd ?? "24 Bulan Resmi"}" : "Masa Garansi Berakhir";
            var buyer = Device?.User?.Name ?? "Pelanggan PT JTS";
            
            var text = $"[PT JAYA TEKNOLOGI SOLUSI - SPESIFIKASI PERANGKAT]\n" +
                       $"• Model Perangkat : {model}\n" +
                       $"• Serial Number   : {serial}\n" +
                       $"• Hardware ID     : {hwId}\n" +
                       $"• Versi BIOS      : {BiosVersion}\n" +
                       $"• Sistem Operasi  : {OsDescription}\n" +
                       $"• Pemakaian RAM   : {MemoryDisplay}\n" +
                       $"• Kapasitas Disk  : {StorageDisplay}\n" +
                       $"• Pemilik Unit    : {buyer}\n" +
                       $"• Status Garansi  : {warrantyStatus}\n" +
                       $"• Tanggal Cek     : {DateTime.Now:dd MMM yyyy HH:mm} WIB\n" +
                       $"• Layanan Support : PT Jaya Teknologi Solusi (Hotline: 0812-3456-7890)";

            _ = CopyToClipboardAsync(text, "✓ Format spesifikasi CS berhasil disalin ke clipboard!");
        }

        [RelayCommand]
        private void CopyAll()
        {
            var model = Device?.Model ?? LocalModel;
            var serial = Device?.SerialNumber ?? LocalSerial;
            var hwId = Device?.HardwareId ?? LocalHardwareId;
            var text = $"Perangkat: {model}\nSerial Number: {serial}\nHardware ID: {hwId}\nVersi BIOS: {BiosVersion}";
            _ = CopyToClipboardAsync(text, "✓ Semua data perangkat berhasil disalin!");
        }

        [RelayCommand]
        private void CopySerial()
        {
            var serial = Device?.SerialNumber ?? LocalSerial;
            _ = CopyToClipboardAsync(serial, $"✓ Serial number {serial} disalin!");
        }

        [RelayCommand]
        private void CopyHardwareId()
        {
            var hwId = Device?.HardwareId ?? LocalHardwareId;
            _ = CopyToClipboardAsync(hwId, "✓ Hardware/BIOS ID disalin!");
        }

        [RelayCommand]
        private void CopyBiosVersion()
        {
            _ = CopyToClipboardAsync(BiosVersion, "✓ Versi BIOS disalin!");
        }

        private async Task CopyToClipboardAsync(string textToCopy, string toastMessage)
        {
            try
            {
                if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow?.Clipboard != null)
                {
                    await desktop.MainWindow.Clipboard.SetTextAsync(textToCopy);
                }
            }
            catch { }

            await ShowToastAsync(toastMessage);
        }

        private async Task ShowToastAsync(string toastMessage)
        {
            CopyToast = toastMessage;
            await Task.Delay(2500);
            CopyToast = null;
        }

        [RelayCommand]
        public void DownloadWarrantyCardImage()
        {
            try
            {
                var model = Device?.Model ?? LocalModel;
                var serial = Device?.SerialNumber ?? LocalSerial;
                var hwId = Device?.HardwareId ?? LocalHardwareId;
                var buyerName = Device?.User?.Name ?? "Pelanggan Terdaftar PT JTS";
                var warrantyEnd = Warranty?.WarrantyEnd ?? "24 Bulan Resmi";
                var purchaseDate = Warranty?.PurchaseDate ?? "Terverifikasi";
                var status = IsWarrantyActive ? "GARANSI RESMI AKTIF" : "MASA GARANSI BERAKHIR";
                var qrToken = Device?.QrToken ?? "JTS-QR-OFFICIAL";

                string savedPath = WarrantyCardImageGenerator.GenerateAndSaveCardImage(
                    model,
                    serial,
                    hwId,
                    buyerName,
                    warrantyEnd,
                    purchaseDate,
                    status,
                    qrToken
                );

                string fileName = Path.GetFileName(savedPath);
                _ = ShowToastAsync($"✓ Gambar Kartu Garansi (PNG) berhasil diunduh ke folder Downloads: {fileName}");

                // Open image safely in OS image viewer without invoking email client
                try
                {
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            Arguments = $"\"{savedPath}\"",
                            UseShellExecute = false
                        });
                    }
                    else
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = savedPath,
                            UseShellExecute = true
                        });
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                _ = ShowToastAsync($"Gagal mengunduh gambar kartu: {ex.Message}");
            }
        }

        [RelayCommand]
        public void ExportWarrantyCertificate()
        {
            try
            {
                var model = Device?.Model ?? LocalModel;
                var serial = Device?.SerialNumber ?? LocalSerial;
                var hwId = Device?.HardwareId ?? LocalHardwareId;
                var buyerName = Device?.User?.Name ?? "Pelanggan Terdaftar PT JTS";
                var warrantyEnd = Warranty?.WarrantyEnd ?? "24 Bulan Resmi";
                var purchaseDate = Warranty?.PurchaseDate ?? "Terverifikasi";
                var status = IsWarrantyActive ? "GARANSI RESMI AKTIF" : "MASA GARANSI BERAKHIR";
                var qrToken = Device?.QrToken ?? "JTS-QR-OFFICIAL";
                string verifyUrl = $"http://127.0.0.1:8000/qr/lookup/{qrToken}";

                string qrBase64 = string.Empty;
                try
                {
                    using var qrGenerator = new QRCoder.QRCodeGenerator();
                    using var qrCodeData = qrGenerator.CreateQrCode(verifyUrl, QRCoder.QRCodeGenerator.ECCLevel.Q);
                    using var qrCode = new QRCoder.PngByteQRCode(qrCodeData);
                    byte[] qrBytes = qrCode.GetGraphic(8, new byte[] { 15, 23, 42 }, new byte[] { 255, 255, 255 });
                    qrBase64 = "data:image/png;base64," + Convert.ToBase64String(qrBytes);
                }
                catch { }

                string html = $@"<!DOCTYPE html>
<html lang=""id"">
<head>
    <meta charset=""UTF-8"">
    <title>Sertifikat Garansi Resmi - PT Jaya Teknologi Solusi</title>
    <style>
        @page {{ size: A4 portrait; margin: 15mm; }}
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #F8FAFC; color: #0F172A; padding: 24px; margin: 0; }}
        .cert-card {{ max-width: 820px; margin: 0 auto; background: #FFFFFF; border: 2px solid #2563EB; border-radius: 12px; padding: 36px; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }}
        .header {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #E2E8F0; padding-bottom: 18px; }}
        .brand {{ display: flex; align-items: center; gap: 14px; }}
        .logo-box {{ background: #2563EB; color: white; padding: 8px 16px; border-radius: 6px; font-weight: 900; font-size: 20px; letter-spacing: 1px; }}
        .title {{ font-size: 20px; font-weight: bold; margin: 0; color: #0F172A; }}
        .subtitle {{ color: #64748B; font-size: 13px; margin-top: 3px; }}
        .status-badge {{ background: #ECFDF5; color: #059669; border: 1.5px solid #10B981; padding: 8px 16px; border-radius: 6px; font-weight: bold; font-size: 13px; }}
        .main-layout {{ display: grid; grid-template-columns: 1fr 220px; gap: 24px; margin: 28px 0; }}
        .grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }}
        .item {{ background: #F8FAFC; border: 1px solid #E2E8F0; border-radius: 6px; padding: 12px 14px; }}
        .label {{ font-size: 10px; color: #64748B; font-weight: bold; text-transform: uppercase; letter-spacing: 0.5px; }}
        .value {{ font-size: 14px; font-weight: bold; color: #0F172A; margin-top: 4px; font-family: Consolas, monospace; }}
        .qr-box {{ background: #F8FAFC; border: 1.5px solid #CBD5E1; border-radius: 8px; padding: 14px; text-align: center; display: flex; flex-direction: column; align-items: center; justify-content: center; }}
        .qr-title {{ font-size: 11px; font-weight: bold; color: #0F172A; margin-bottom: 8px; }}
        .qr-img {{ width: 140px; height: 140px; border-radius: 4px; border: 1px solid #E2E8F0; }}
        .token-text {{ font-size: 11px; color: #2563EB; font-weight: bold; font-family: Consolas, monospace; margin-top: 6px; }}
        .footer {{ border-top: 1px solid #E2E8F0; padding-top: 18px; display: flex; justify-content: space-between; align-items: center; color: #64748B; font-size: 12px; }}
        .seal {{ color: #2563EB; font-weight: bold; font-size: 12px; }}
        .actions {{ text-align: center; margin-top: 24px; }}
        .print-btn {{ background: #2563EB; color: white; border: none; padding: 10px 24px; border-radius: 6px; font-size: 13px; font-weight: bold; cursor: pointer; }}
        @media print {{ .actions {{ display: none; }} body {{ background: white; padding: 0; }} .cert-card {{ border: 1.5px solid #2563EB; box-shadow: none; }} }}
    </style>
</head>
<body>
    <div class=""cert-card"">
        <div class=""header"">
            <div class=""brand"">
                <div class=""logo-box"">PT JTS</div>
                <div>
                    <div class=""title"">SERTIFIKAT JAMINAN GARANSI RESMI</div>
                    <div class=""subtitle"">Sistem Garansi Hardware &amp; Dukungan Pelanggan Resmi</div>
                </div>
            </div>
            <div class=""status-badge"">✓ {status}</div>
        </div>

        <div class=""main-layout"">
            <div class=""grid"">
                <div class=""item"">
                    <div class=""label"">Model Perangkat</div>
                    <div class=""value"">{model}</div>
                </div>
                <div class=""item"">
                    <div class=""label"">Nomor Seri (Serial Number)</div>
                    <div class=""value"" style=""color:#2563EB;"">{serial}</div>
                </div>
                <div class=""item"" style=""grid-column: span 2;"">
                    <div class=""label"">Identitas BIOS / Hardware ID</div>
                    <div class=""value"" style=""font-size:12px; word-break: break-all;"">{hwId}</div>
                </div>
                <div class=""item"">
                    <div class=""label"">Nama Pemilik Terdaftar</div>
                    <div class=""value"">{buyerName}</div>
                </div>
                <div class=""item"">
                    <div class=""label"">Masa Berlaku Garansi</div>
                    <div class=""value"" style=""color:#059669;"">{warrantyEnd}</div>
                </div>
            </div>

            <div class=""qr-box"">
                <div class=""qr-title"">PINDAI VERIFIKASI</div>
                {(string.IsNullOrEmpty(qrBase64) ? "" : $"<img class=\"qr-img\" src=\"{qrBase64}\" alt=\"QR Code Verifikasi\" />")}
                <div class=""token-text"">{qrToken}</div>
                <div style=""font-size: 9px; color: #94A3B8; margin-top: 4px;"">Pindai dengan kamera smartphone</div>
            </div>
        </div>

        <div class=""footer"">
            <div>
                <strong>PT Jaya Teknologi Solusi</strong><br>
                Hotline Servis Resmi: 0812-9900-1122 • Dukungan Remote &amp; On-Site
            </div>
            <div class=""seal"">
                ★ JTS VERIFIED HARDWARE GUARANTEE ★
            </div>
        </div>

        <div class=""actions"">
            <button class=""print-btn"" onclick=""window.print()"">🖨️ Cetak Sertifikat / Simpan PDF</button>
        </div>
    </div>
</body>
</html>";

                string tempPath = Path.Combine(Path.GetTempPath(), $"Sertifikat_Garansi_JTS_{serial}.html");
                File.WriteAllText(tempPath, html);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });

                _ = ShowToastAsync("Sertifikat Garansi (Clean Light) berhasil dibuka!");
            }
            catch (Exception ex)
            {
                _ = ShowToastAsync($"Gagal mengekspor sertifikat: {ex.Message}");
            }
        }

        [RelayCommand]
        public void GoToDiagnostic() => _navigate("Diagnostic");

        [RelayCommand]
        public void OpenStorageDetail() => _navigate("ComponentDetail_Storage");

        [RelayCommand]
        public void OpenRamDetail() => _navigate("ComponentDetail_RAM");

        [RelayCommand]
        public void OpenCpuDetail() => _navigate("ComponentDetail_CPU");

        [RelayCommand]
        private void GoToClaim() => _navigate("Claim");

        [RelayCommand]
        private void GoToTracking() => _navigate("Tracking");

        [RelayCommand]
        private void GoToLocation() => _navigate("Location");
    }
}
