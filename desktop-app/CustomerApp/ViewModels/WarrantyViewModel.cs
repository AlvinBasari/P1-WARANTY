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

        public string DisplayModel => !string.IsNullOrWhiteSpace(Device?.Model) ? Device.Model : (!string.IsNullOrWhiteSpace(LocalModel) ? LocalModel : "Unit Hardware");
        public string DisplaySerialNumber => !string.IsNullOrWhiteSpace(Device?.SerialNumber) ? Device.SerialNumber : (!string.IsNullOrWhiteSpace(LocalSerial) ? LocalSerial : "-");
        public string DisplayHardwareId => !string.IsNullOrWhiteSpace(Device?.HardwareId) ? Device.HardwareId : (!string.IsNullOrWhiteSpace(LocalHardwareId) ? LocalHardwareId : "-");

        partial void OnLocalModelChanged(string value) => OnPropertyChanged(nameof(DisplayModel));
        partial void OnLocalSerialChanged(string value) => OnPropertyChanged(nameof(DisplaySerialNumber));
        partial void OnLocalHardwareIdChanged(string value) => OnPropertyChanged(nameof(DisplayHardwareId));

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

            OnPropertyChanged(nameof(DisplayModel));
            OnPropertyChanged(nameof(DisplaySerialNumber));
            OnPropertyChanged(nameof(DisplayHardwareId));
        }

        [RelayCommand]
        public void OpenRepairHistory()
        {
            _navigate("Tracking");
        }

        [RelayCommand]
        public async Task ActivateWithTokenAsync()
        {
            if (IsActivating) return;

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
        @page {{ size: A4 portrait; margin: 12mm 15mm; }}
        * {{ box-sizing: border-box; margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; }}
        body {{ background: #F8FAFC; color: #0F172A; padding: 24px; margin: 0; font-size: 12px; line-height: 1.5; }}
        .cert-card {{ max-width: 820px; margin: 0 auto; background: #FFFFFF; border: 1.5px solid #0F172A; border-radius: 4px; padding: 32px 36px; box-shadow: 0 4px 16px rgba(15,23,42,0.06); }}
        
        /* Kop Surat Resmi */
        .kop {{ display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #0F172A; padding-bottom: 16px; }}
        .kop-brand {{ display: flex; align-items: center; gap: 10px; margin-bottom: 4px; }}
        .kop-logo {{ background: #0F172A; color: white; padding: 3px 8px; border-radius: 3px; font-weight: 800; font-size: 13px; font-family: 'Consolas', monospace; }}
        .kop-company {{ font-size: 17px; font-weight: 800; color: #0F172A; letter-spacing: 0.2px; }}
        .kop-dept {{ font-size: 10.5px; font-weight: 600; color: #475569; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 3px; }}
        .kop-meta {{ font-size: 10px; color: #64748B; line-height: 1.4; }}
        
        .cert-badge-box {{ text-align: right; min-width: 240px; }}
        .cert-doc-title {{ font-size: 14px; font-weight: 800; color: #0F172A; text-transform: uppercase; letter-spacing: 0.8px; }}
        .cert-doc-sub {{ font-size: 10px; color: #64748B; font-weight: 600; margin-top: 2px; }}
        .cert-status {{ display: inline-block; margin-top: 6px; padding: 4px 10px; border: 1.5px solid #0F172A; border-radius: 3px; font-size: 11px; font-weight: 800; background: #F8FAFC; color: #0F172A; }}
        
        /* Main Layout Grid */
        .main-layout {{ display: grid; grid-template-columns: 1fr 210px; gap: 20px; margin: 22px 0; }}
        .specs-grid {{ display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }}
        .spec-item {{ background: #FFFFFF; border: 1px solid #CBD5E1; border-radius: 3px; overflow: hidden; }}
        .spec-label {{ background: #F8FAFC; border-bottom: 1px solid #E2E8F0; padding: 5px 10px; font-size: 9.5px; color: #475569; font-weight: 700; text-transform: uppercase; letter-spacing: 0.5px; }}
        .spec-val {{ padding: 8px 10px; font-size: 12.5px; font-weight: 700; color: #0F172A; font-family: 'Consolas', monospace; }}
        
        .qr-card {{ background: #FFFFFF; border: 1px solid #CBD5E1; border-radius: 3px; padding: 12px; text-align: center; display: flex; flex-direction: column; align-items: center; justify-content: center; }}
        .qr-header {{ font-size: 9.5px; font-weight: 800; color: #0F172A; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; border-bottom: 1px solid #E2E8F0; width: 100%; padding-bottom: 4px; }}
        .qr-img {{ width: 130px; height: 130px; border: 1px solid #E2E8F0; border-radius: 2px; }}
        .qr-token {{ font-size: 11px; color: #0F172A; font-weight: 800; font-family: 'Consolas', monospace; margin-top: 6px; }}
        .qr-note {{ font-size: 9px; color: #64748B; margin-top: 2px; }}
        
        /* Terms */
        .terms-box {{ background: #F8FAFC; border-left: 3px solid #0F172A; padding: 8px 12px; font-size: 10px; color: #475569; margin-bottom: 20px; line-height: 1.5; }}
        .terms-box strong {{ color: #0F172A; display: block; margin-bottom: 2px; text-transform: uppercase; font-size: 9.5px; }}
        
        /* Signatures */
        .signatures {{ display: grid; grid-template-columns: 1fr 1fr; gap: 30px; padding-top: 14px; border-top: 1px solid #CBD5E1; margin-bottom: 16px; text-align: center; }}
        .sig-title {{ font-size: 10px; font-weight: 700; color: #475569; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 6px; }}
        .sig-space {{ height: 60px; display: flex; align-items: center; justify-content: center; }}
        .sig-stamp {{ border: 1.5px solid #0F172A; padding: 6px 12px; background: #F8FAFC; display: inline-block; font-size: 9px; font-weight: 800; color: #0F172A; }}
        .sig-name {{ font-size: 11px; font-weight: 700; color: #0F172A; border-top: 1px solid #94A3B8; padding-top: 4px; display: inline-block; min-width: 180px; }}
        
        /* Footer */
        .footer {{ border-top: 1px solid #E2E8F0; padding-top: 12px; display: flex; justify-content: space-between; align-items: center; font-size: 10px; color: #64748B; }}
        .footer-seal {{ font-weight: 800; color: #0F172A; text-transform: uppercase; letter-spacing: 0.5px; }}
        
        .actions {{ text-align: center; margin-top: 24px; }}
        .print-btn {{ background: #0F172A; color: white; border: none; padding: 9px 22px; border-radius: 4px; font-size: 12px; font-weight: 700; cursor: pointer; letter-spacing: 0.3px; }}
        .print-btn:hover {{ background: #334155; }}
        @media print {{ .actions {{ display: none; }} body {{ background: white; padding: 0; }} .cert-card {{ border: 1.5px solid #0F172A; box-shadow: none; max-width: 100%; }} }}
    </style>
</head>
<body>
    <div class=""cert-card"">
        <!-- Kop Surat -->
        <div class=""kop"">
            <div>
                <div class=""kop-brand"">
                    <span class=""kop-logo"">PT JTS</span>
                    <span class=""kop-company"">PT JAYA TEKNOLOGI SOLUSI</span>
                </div>
                <div class=""kop-dept"">Divisi Layanan Purna Jual &amp; Jaminan Garansi Resmi Hardware</div>
                <div class=""kop-meta"">
                    Gedung Cyber 2 Tower Lt. 18, Jl. H.R. Rasuna Said Blok X-5, Jakarta Selatan 12950<br>
                    Telepon: (021) 5088-7799 • Email: aftersales@jts.co.id • NPWP: 01.884.223.4-015.000
                </div>
            </div>
            <div class=""cert-badge-box"">
                <div class=""cert-doc-title"">SERTIFIKAT GARANSI RESMI</div>
                <div class=""cert-doc-sub"">No. Registrasi: JTS-GAR/{serial}</div>
                <div class=""cert-status"">{status}</div>
            </div>
        </div>

        <!-- Specifications & QR -->
        <div class=""main-layout"">
            <div class=""specs-grid"">
                <div class=""spec-item"">
                    <div class=""spec-label"">Model Perangkat / Unit</div>
                    <div class=""spec-val"">{model}</div>
                </div>
                <div class=""spec-item"">
                    <div class=""spec-label"">Nomor Seri Unit (S/N)</div>
                    <div class=""spec-val"">{serial}</div>
                </div>
                <div class=""spec-item"" style=""grid-column: span 2;"">
                    <div class=""spec-label"">Identitas BIOS / Hardware ID (UUID)</div>
                    <div class=""spec-val"" style=""font-size: 11px; word-break: break-all;"">{hwId}</div>
                </div>
                <div class=""spec-item"">
                    <div class=""spec-label"">Nama Pemilik Terdaftar</div>
                    <div class=""spec-val"">{buyerName}</div>
                </div>
                <div class=""spec-item"">
                    <div class=""spec-label"">Periode Garansi Berlaku</div>
                    <div class=""spec-val"">{warrantyEnd}</div>
                </div>
            </div>

            <div class=""qr-card"">
                <div class=""qr-header"">VERIFIKASI KEABSAHAN</div>
                {(string.IsNullOrEmpty(qrBase64) ? "" : $"<img class=\"qr-img\" src=\"{qrBase64}\" alt=\"QR Code Verifikasi\" />")}
                <div class=""qr-token"">{qrToken}</div>
                <div class=""qr-note"">Pindai untuk validasi data cloud</div>
            </div>
        </div>

        <!-- Terms Box -->
        <div class=""terms-box"">
            <strong>Ketentuan Garansi Resmi PT JTS:</strong>
            Sertifikat ini adalah bukti sah bahwa unit komputer/laptop dengan nomor seri tertera dilindungi oleh jaminan servis resmi PT Jaya Teknologi Solusi. Cakupan mencakup penggantian suku cadang OEM dan jasa teknisi (remote support / on-site workshop) sesuai masa aktif garansi.
        </div>

        <!-- Signatures Section -->
        <div class=""signatures"">
            <div>
                <div class=""sig-title"">Pemilik / Pemegang Garansi</div>
                <div class=""sig-space"">
                    <span style=""font-size: 9.5px; color: #94A3B8; font-style: italic;"">(Tercatat di sistem registrasi)</span>
                </div>
                <div class=""sig-name"">{buyerName}</div>
            </div>

            <div>
                <div class=""sig-title"">Pengesahan Resmi PT JTS</div>
                <div class=""sig-space"">
                    <div class=""sig-stamp"">
                        PT JAYA TEKNOLOGI SOLUSI<br>
                        DIGITALLY VERIFIED HARDWARE CERTIFICATE
                    </div>
                </div>
                <div class=""sig-name"">Divisi Layanan Purna Jual PT JTS</div>
            </div>
        </div>

        <!-- Footer -->
        <div class=""footer"">
            <div>
                Layanan Bantuan Resmi: (021) 5088-7799 • aftersales@jts.co.id
            </div>
            <div class=""footer-seal"">
                DOKUMEN RESMI PT JAYA TEKNOLOGI SOLUSI
            </div>
        </div>

        <div class=""actions"">
            <button class=""print-btn"" onclick=""window.print()"">Cetak Sertifikat / Simpan PDF</button>
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
