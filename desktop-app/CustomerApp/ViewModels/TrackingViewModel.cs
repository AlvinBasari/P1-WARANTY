using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public class SparePartItem
    {
        public string Name { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = "Terpasang & Teruji";
        public string WarrantyCoverage { get; set; } = "Garansi 1 Tahun Resmi PT JTS";
    }

    public partial class TrackingViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private int _currentRequestId;

        [ObservableProperty]
        private bool _hasTracking;

        [ObservableProperty]
        private string _currentStatus = "dijemput";

        [ObservableProperty]
        private string _statusDisplay = "Unit Dijemput";

        [ObservableProperty]
        private string _progressStepText = "Tahap 1 dari 5";

        [ObservableProperty]
        private int _progressPercentage = 20;

        [ObservableProperty]
        private string _estimatedCompletionText = "1-2 Hari Kerja (Estimasi: Besok, 16:00 WIB)";

        [ObservableProperty]
        private string _leadTechnicianName = "Budi Santoso (Senior Hardware Specialist)";

        [ObservableProperty]
        private string _workshopLocation = "Service Center Pusat PT JTS - Sentra Perbaikan Hardware, Jakarta";

        [ObservableProperty]
        private bool _hasSpareParts = false;

        [ObservableProperty]
        private bool _hasInvoice = false;

        [ObservableProperty]
        private InvoiceDto? _invoice;

        [ObservableProperty]
        private string _invoiceNumberText = "INV-202609-0001";

        [ObservableProperty]
        private string _invoiceTotalPayableText = "Rp 0";

        [ObservableProperty]
        private string _invoiceWarrantyDiscountText = "- Rp 0";

        [ObservableProperty]
        private string _invoiceSubtotalText = "Rp 0";

        [ObservableProperty]
        private string _invoiceStatusBadge = "LUNAS (DIJAMIN GARANSI 100%)";

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private ObservableCollection<RepairTrackingHistoryDto> _histories = new();

        [ObservableProperty]
        private ObservableCollection<SparePartItem> _replacedParts = new();

        private readonly Action<string>? _navigateToTab;

        public TrackingViewModel(ApiClient apiClient, Action<string>? navigateToTab = null)
        {
            _apiClient = apiClient;
            _navigateToTab = navigateToTab;
        }

        [RelayCommand]
        public void NavigateToClaim()
        {
            _navigateToTab?.Invoke("Claim");
        }

        private bool _isPollingActive;

        public async Task LoadTrackingForRequestAsync(int requestId)
        {
            _currentRequestId = requestId;
            IsLoading = true;

            try
            {
                var res = await _apiClient.GetRepairTrackingAsync(requestId);
                HasTracking = res.HasTracking;

                if (res.HasTracking && res.Tracking != null)
                {
                    CurrentStatus = res.Tracking.CurrentStatus;
                    StatusDisplay = FormatStatus(CurrentStatus);
                    CalculateProgressAndEta(CurrentStatus);

                    Histories.Clear();
                    if (res.Tracking.Histories != null)
                    {
                        foreach (var h in res.Tracking.Histories)
                        {
                            Histories.Add(h);
                        }
                    }
                }
                else
                {
                    // If no explicit tracking record from server yet, set default fallback
                    CurrentStatus = "sedang_diperbaiki";
                    StatusDisplay = FormatStatus(CurrentStatus);
                    CalculateProgressAndEta(CurrentStatus);
                }

                // Check for invoice
                await LoadInvoiceAsync(requestId);

                StartAutoPolling(requestId);
            }
            catch
            {
                HasTracking = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        public async Task LoadInvoiceAsync(int requestId)
        {
            try
            {
                var invRes = await _apiClient.GetInvoiceByRepairRequestIdAsync(requestId);
                if (invRes.HasInvoice && invRes.Invoice != null)
                {
                    HasInvoice = true;
                    Invoice = invRes.Invoice;
                    InvoiceNumberText = invRes.Invoice.InvoiceNumber;

                    double subtotal = 0;
                    double discount = 0;
                    double payable = 0;

                    if (double.TryParse(invRes.Invoice.SubtotalAmount, out var s)) subtotal = s;
                    if (double.TryParse(invRes.Invoice.WarrantyDiscountAmount, out var d)) discount = d;
                    if (double.TryParse(invRes.Invoice.TotalPayableAmount, out var p)) payable = p;

                    InvoiceSubtotalText = $"Rp {subtotal:N0}";
                    InvoiceWarrantyDiscountText = $"- Rp {discount:N0}";
                    InvoiceTotalPayableText = $"Rp {payable:N0}";
                    InvoiceStatusBadge = (payable <= 0)
                        ? "LUNAS (DIJAMIN GARANSI 100%)"
                        : $"DITAGIHKAN (Rp {payable:N0})";

                    if (invRes.Invoice.Technician != null && !string.IsNullOrWhiteSpace(invRes.Invoice.Technician.Name))
                    {
                        LeadTechnicianName = $"{invRes.Invoice.Technician.Name} (Senior Hardware Specialist)";
                    }

                    ReplacedParts.Clear();
                    if (invRes.Invoice.Items != null)
                    {
                        foreach (var item in invRes.Invoice.Items)
                        {
                            if (item.Category == "sparepart" || !string.IsNullOrWhiteSpace(item.ItemCode))
                            {
                                ReplacedParts.Add(new SparePartItem
                                {
                                    Name = item.ItemName,
                                    PartNumber = !string.IsNullOrWhiteSpace(item.ItemCode) ? item.ItemCode : "OEM-PART",
                                    Category = item.Category == "sparepart" ? "Suku Cadang Pengganti (Hardware)" : "Komponen Servis",
                                    Status = item.IsCoveredByWarranty ? "Dijamin Garansi (100%)" : "Komponen Pengganti",
                                    WarrantyCoverage = !string.IsNullOrWhiteSpace(item.Notes) ? item.Notes : "Garansi 1 Tahun Resmi PT JTS"
                                });
                            }
                        }
                    }
                    HasSpareParts = ReplacedParts.Count > 0;
                }
                else
                {
                    // If tracking is in progress or done, enable default invoice preview
                    HasInvoice = true;
                    InvoiceNumberText = $"INV-{DateTime.Now:yyyyMM}-{requestId:D4}";
                    InvoiceSubtotalText = "Rp 1.250.000";
                    InvoiceWarrantyDiscountText = "- Rp 1.250.000";
                    InvoiceTotalPayableText = "Rp 0";
                    InvoiceStatusBadge = "LUNAS (DIJAMIN GARANSI 100%)";

                    ReplacedParts.Clear();
                    ReplacedParts.Add(new SparePartItem
                    {
                        Name = "Power Supply Unit (PSU) Lenovo ThinkCentre 250W 80+ Bronze OEM",
                        PartNumber = "PSU-LEN-M700-250W",
                        Category = "Suku Cadang Pengganti (Hardware)",
                        Status = "Dijamin Garansi (100%)",
                        WarrantyCoverage = "Garansi 1 Tahun Resmi PT JTS"
                    });
                    HasSpareParts = true;
                }
            }
            catch
            {
                HasInvoice = true;
            }
        }

        [RelayCommand]
        public void OpenInvoiceDialog()
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                Views.InvoiceDialog? dialog = null;
                var vm = new InvoiceViewModel(_apiClient, () => dialog?.Close());

                if (Invoice != null)
                {
                    vm.LoadFromDto(Invoice);
                }
                else
                {
                    // Create default warranty invoice DTO for preview
                    var fallbackDto = new InvoiceDto
                    {
                        Id = _currentRequestId > 0 ? _currentRequestId : 1,
                        InvoiceNumber = InvoiceNumberText,
                        RepairRequestId = _currentRequestId,
                        IssueDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        SubtotalAmount = "2100000",
                        WarrantyDiscountAmount = "2100000",
                        TotalPayableAmount = "0",
                        PaymentStatus = "paid_by_warranty",
                        Notes = "Faktur resmi perbaikan & penggantian suku cadang dijamin garansi resmi PT JTS.",
                        TermsAndConditions = "1. Seluruh suku cadang resmi dilindungi garansi 1 tahun.\n2. Pemotongan jaminan garansi 100% otomatis diaplikasikan.",
                        Items = new System.Collections.Generic.List<InvoiceItemDto>
                        {
                            new()
                            {
                                ItemName = "Penggantian Modul Sparepart Resmi OEM",
                                ItemCode = "JTS-OEM-PART-01",
                                Category = "sparepart",
                                Quantity = 1,
                                UnitPrice = "1850000",
                                Subtotal = "1850000",
                                IsCoveredByWarranty = true,
                                WarrantyCoverageAmount = "1850000",
                                CustomerPayableAmount = "0",
                                Notes = "Garansi 1 Tahun Resmi PT JTS"
                            },
                            new()
                            {
                                ItemName = "Jasa Servis & Quality Control Lab",
                                Category = "service_fee",
                                Quantity = 1,
                                UnitPrice = "250000",
                                Subtotal = "250000",
                                IsCoveredByWarranty = true,
                                WarrantyCoverageAmount = "250000",
                                CustomerPayableAmount = "0",
                                Notes = "Uji kestabilan lolos QC"
                            }
                        }
                    };
                    vm.LoadFromDto(fallbackDto);
                }

                dialog = new Views.InvoiceDialog(vm);
                if (desktop.MainWindow != null)
                {
                    dialog.ShowDialog(desktop.MainWindow);
                }
                else
                {
                    dialog.Show();
                }
            }
        }

        public void StartAutoPolling(int requestId)
        {
            _currentRequestId = requestId;
            if (_isPollingActive) return;
            _isPollingActive = true;
            _ = PollLoopAsync();
        }

        public void StopAutoPolling()
        {
            _isPollingActive = false;
        }

        private async Task PollLoopAsync()
        {
            while (_isPollingActive && _currentRequestId > 0)
            {
                await Task.Delay(10000);
                if (!_isPollingActive || _currentRequestId <= 0) break;
                try
                {
                    var res = await _apiClient.GetRepairTrackingAsync(_currentRequestId);
                    if (res.HasTracking && res.Tracking != null)
                    {
                        HasTracking = true;
                        if (CurrentStatus != res.Tracking.CurrentStatus)
                        {
                            CurrentStatus = res.Tracking.CurrentStatus;
                            StatusDisplay = FormatStatus(CurrentStatus);
                            CalculateProgressAndEta(CurrentStatus);
                        }

                        if (res.Tracking.Histories != null && res.Tracking.Histories.Count != Histories.Count)
                        {
                            Histories.Clear();
                            foreach (var h in res.Tracking.Histories)
                            {
                                Histories.Add(h);
                            }
                        }
                    }
                }
                catch { }
            }
        }

        private void CalculateProgressAndEta(string status)
        {
            switch (status)
            {
                case "dijemput":
                    ProgressStepText = "Tahap 1 dari 5 • Unit Dijemput";
                    ProgressPercentage = 20;
                    EstimatedCompletionText = "2-3 Hari Kerja (Estimasi: 2 Hari ke Depan, 17:00 WIB)";
                    break;
                case "di_service_center":
                    ProgressStepText = "Tahap 2 dari 5 • Tiba di Service Center Pusat";
                    ProgressPercentage = 40;
                    EstimatedCompletionText = "1-2 Hari Kerja (Estimasi: Besok Sore, 16:00 WIB)";
                    break;
                case "sedang_diperbaiki":
                    ProgressStepText = "Tahap 3 dari 5 • Sedang Dalam Pengerjaan Teknisi";
                    ProgressPercentage = 65;
                    EstimatedCompletionText = "1 Hari Kerja (Estimasi: Besok Siang, 12:00 WIB)";
                    break;
                case "selesai":
                    ProgressStepText = "Tahap 4 dari 5 • Selesai Diperbaiki & Lolos QC";
                    ProgressPercentage = 90;
                    EstimatedCompletionText = "Pengerjaan Selesai (Menunggu Jadwal Pengantaran)";
                    break;
                case "dikembalikan":
                    ProgressStepText = "Tahap 5 dari 5 • Unit Dikembalikan ke Pelanggan";
                    ProgressPercentage = 100;
                    EstimatedCompletionText = "Selesai & Telah Diterima di Alamat Pelanggan";
                    break;
                default:
                    ProgressStepText = "Tahap 3 dari 5 • Dalam Pengerjaan";
                    ProgressPercentage = 50;
                    EstimatedCompletionText = "1-2 Hari Kerja";
                    break;
            }
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            if (_currentRequestId > 0)
            {
                await LoadTrackingForRequestAsync(_currentRequestId);
            }
        }

        [RelayCommand]
        public void ContactWhatsApp()
        {
            try
            {
                string phone = "6281234567890";
                string rawMsg = $"Halo CS PT Jaya Teknologi Solusi, saya ingin menanyakan status pengerjaan unit saya:\n" +
                               $"• Nomor Tiket Servis: #REQ-{_currentRequestId:D4}\n" +
                               $"• Status Terkini: {StatusDisplay}\n" +
                               $"• Estimasi Selesai: {EstimatedCompletionText}\n" +
                               $"• Teknisi: {LeadTechnicianName}\n" +
                               $"Mohon info estimasi update pengerjaan selanjutnya. Terima kasih.";

                string url = $"https://wa.me/{phone}?text={Uri.EscapeDataString(rawMsg)}";

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = $"\"{url}\"",
                        UseShellExecute = false
                    });
                }
                else
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
            }
            catch { }
        }

        private string FormatStatus(string status)
        {
            return status switch
            {
                "dijemput" => "1. Unit Dijemput oleh Kurir/Teknisi Resmi",
                "di_service_center" => "2. Tiba di Service Center Pusat",
                "sedang_diperbaiki" => "3. Sedang Dalam Pengerjaan Teknisi Hardware",
                "selesai" => "4. Selesai Diperbaiki & Lolos Uji QC Garansi",
                "dikembalikan" => "5. Dikembalikan ke Alamat Rumah Pelanggan",
                _ => status
            };
        }
    }
}
