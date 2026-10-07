using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace CustomerApp.ViewModels
{
    public class InvoiceItemDisplayModel
    {
        public string ItemName { get; set; } = string.Empty;
        public string ItemCode { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string UnitPriceFormatted { get; set; } = "Rp 0";
        public string SubtotalFormatted { get; set; } = "Rp 0";
        public bool IsCoveredByWarranty { get; set; } = true;
        public string WarrantyBadgeText => IsCoveredByWarranty ? "🛡️ TERCOVER GARANSI (100%)" : "⚠️ BIAYA KLIEN";
        public string CustomerPayableFormatted { get; set; } = "Rp 0";
        public string Notes { get; set; } = string.Empty;
    }

    public partial class InvoiceViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly Action _onClose;

        [ObservableProperty]
        private InvoiceDto? _invoice;

        [ObservableProperty]
        private string _invoiceNumber = "INV-202609-0000";

        [ObservableProperty]
        private string _issueDateText = "-";

        [ObservableProperty]
        private string _customerName = "-";

        [ObservableProperty]
        private string _customerAddress = "-";

        [ObservableProperty]
        private string _deviceModel = "-";

        [ObservableProperty]
        private string _serialNumber = "-";

        [ObservableProperty]
        private string _hardwareId = "-";

        [ObservableProperty]
        private string _technicianName = "Tim Servis Resmi PT JTS";

        [ObservableProperty]
        private string _subtotalFormatted = "Rp 0";

        [ObservableProperty]
        private string _warrantyDiscountFormatted = "- Rp 0";

        [ObservableProperty]
        private string _totalPayableFormatted = "Rp 0";

        [ObservableProperty]
        private bool _isFullyCoveredByWarranty = true;

        [ObservableProperty]
        private string _paymentStatusBadge = "LUNAS (DIJAMIN GARANSI 100%)";

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private string _terms = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private ObservableCollection<InvoiceItemDisplayModel> _displayItems = new();

        public InvoiceViewModel(ApiClient apiClient, Action onClose)
        {
            _apiClient = apiClient;
            _onClose = onClose;
        }

        public void LoadFromDto(InvoiceDto inv)
        {
            Invoice = inv;
            InvoiceNumber = inv.InvoiceNumber ?? $"INV-{inv.Id}";
            
            if (DateTime.TryParse(inv.IssueDate, out var dt))
            {
                IssueDateText = dt.ToString("dd MMMM yyyy, HH:mm") + " WIB";
            }
            else
            {
                IssueDateText = inv.IssueDate ?? DateTime.Now.ToString("dd MMMM yyyy");
            }

            CustomerName = inv.User?.Name ?? "Pelanggan JTS";
            CustomerAddress = inv.User?.Address ?? "Alamat Terkalibrasi GPS";
            DeviceModel = inv.Device?.Model ?? "Perangkat Terdaftar";
            SerialNumber = inv.Device?.SerialNumber ?? "-";
            HardwareId = inv.Device?.HardwareId ?? "-";
            TechnicianName = inv.Technician?.Name ?? "Tim Servis Resmi PT JTS";

            double subtotal = 0;
            double discount = 0;
            double payable = 0;

            if (double.TryParse(inv.SubtotalAmount, out var s)) subtotal = s;
            if (double.TryParse(inv.WarrantyDiscountAmount, out var d)) discount = d;
            if (double.TryParse(inv.TotalPayableAmount, out var p)) payable = p;

            SubtotalFormatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Rp {0:N0}", subtotal);
            WarrantyDiscountFormatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, "- Rp {0:N0}", discount);
            TotalPayableFormatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Rp {0:N0}", payable);

            IsFullyCoveredByWarranty = (payable <= 0);
            PaymentStatusBadge = IsFullyCoveredByWarranty
                ? "LUNAS (KLAIM GARANSI PENUH)"
                : $"MENUNGGU PEMBAYARAN ({TotalPayableFormatted})";

            Notes = inv.Notes ?? "Faktur resmi jaminan garansi dan perbaikan perangkat PT Jaya Teknologi Solusi.";
            Terms = inv.TermsAndConditions ?? "1. Seluruh suku cadang resmi dilindungi garansi 1 tahun.\n2. Biaya yang dijamin garansi telah dipotong 100% otomatis.\n3. Simpan dokumen ini sebagai bukti klaim sah.";

            DisplayItems.Clear();
            if (inv.Items != null)
            {
                foreach (var item in inv.Items)
                {
                    double unitPrice = 0;
                    double itemSubtotal = 0;
                    double itemPayable = 0;

                    if (double.TryParse(item.UnitPrice, out var up)) unitPrice = up;
                    if (double.TryParse(item.Subtotal, out var sub)) itemSubtotal = sub;
                    if (double.TryParse(item.CustomerPayableAmount, out var cp)) itemPayable = cp;

                    DisplayItems.Add(new InvoiceItemDisplayModel
                    {
                        ItemName = item.ItemName,
                        ItemCode = item.ItemCode ?? "",
                        CategoryName = FormatCategory(item.Category),
                        Quantity = item.Quantity,
                        UnitPriceFormatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Rp {0:N0}", unitPrice),
                        SubtotalFormatted = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Rp {0:N0}", itemSubtotal),
                        IsCoveredByWarranty = item.IsCoveredByWarranty,
                        CustomerPayableFormatted = item.IsCoveredByWarranty ? "Rp 0 (Covered)" : string.Format(System.Globalization.CultureInfo.InvariantCulture, "Rp {0:N0}", itemPayable),
                        Notes = item.Notes ?? ""
                    });
                }
            }
        }

        private string FormatCategory(string cat)
        {
            return cat switch
            {
                "sparepart" => "Suku Cadang",
                "service_fee" => "Jasa Servis",
                "diagnostic_fee" => "Diagnostik",
                "transport_fee" => "On-Site Transport",
                _ => "Lainnya"
            };
        }

        [RelayCommand]
        public async Task DownloadAndOpenInvoiceAsync()
        {
            if (IsLoading || Invoice == null) return;

            IsLoading = true;
            StatusMessage = "Mengunduh berkas invoice resmi...";

            try
            {
                string htmlContent;
                try
                {
                    htmlContent = await _apiClient.DownloadInvoiceHtmlAsync(Invoice.Id);
                }
                catch
                {
                    // Fallback local HTML generator if offline
                    htmlContent = GenerateFallbackHtml(Invoice);
                }

                string downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string saveDir = Path.Combine(downloadsFolder, "Downloads");
                if (!Directory.Exists(saveDir))
                {
                    saveDir = Path.Combine(downloadsFolder, "Documents");
                }

                string rawNum = string.IsNullOrWhiteSpace(Invoice.InvoiceNumber) ? $"INV-{Invoice.Id}" : Invoice.InvoiceNumber;
                string safeNum = string.Join("_", rawNum.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(saveDir, $"Faktur-Garansi-{safeNum}.html");

                try
                {
                    await File.WriteAllTextAsync(filePath, htmlContent);
                }
                catch (IOException)
                {
                    filePath = Path.Combine(saveDir, $"Faktur-Garansi-{safeNum}_{DateTime.Now:yyyyMMddHHmmssfff}.html");
                    await File.WriteAllTextAsync(filePath, htmlContent);
                }

                StatusMessage = $"✓ Invoice tersimpan di: {Path.GetFileName(filePath)}";

                // Open in default browser / system handler
                OpenFileWithDefaultApp(filePath);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal menyimpan invoice: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Close()
        {
            _onClose();
        }

        private static void OpenFileWithDefaultApp(string filePath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{filePath}\"") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start(new ProcessStartInfo("xdg-open", $"\"{filePath}\"") { UseShellExecute = false });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start(new ProcessStartInfo("open", $"\"{filePath}\"") { UseShellExecute = false });
                }
            }
            catch { }
        }

        private string GenerateFallbackHtml(InvoiceDto inv)
        {
            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'/>
<title>Faktur Servis Resmi - {inv.InvoiceNumber}</title>
<style>
body {{ font-family: sans-serif; padding: 30px; color: #1e293b; }}
.header {{ border-bottom: 2px solid #0284c7; padding-bottom: 15px; margin-bottom: 20px; }}
.title {{ font-size: 20px; font-weight: bold; color: #0284c7; }}
table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
th, td {{ padding: 10px; border-bottom: 1px solid #e2e8f0; text-align: left; }}
th {{ background: #f8fafc; font-size: 12px; }}
.total {{ font-weight: bold; font-size: 16px; color: #166534; }}
</style>
</head>
<body>
<div class='header'>
  <div class='title'>PT JAYA TEKNOLOGI SOLUSI - FAKTUR KLAIM GARANSI</div>
  <div>No. Faktur: <strong>{inv.InvoiceNumber}</strong> • Tanggal: {IssueDateText}</div>
  <div>Perangkat: {DeviceModel} (S/N: {SerialNumber})</div>
</div>
<h3>Rincian Tindakan & Suku Cadang</h3>
<table>
  <thead>
    <tr><th>Item</th><th>Qty</th><th>Harga</th><th>Garansi</th><th>Ditagih</th></tr>
  </thead>
  <tbody>
    <tr><td>Perbaikan Hardware & Diagnostik Lengkap</td><td>1</td><td>{SubtotalFormatted}</td><td>Jaminan Garansi 100%</td><td>{TotalPayableFormatted}</td></tr>
  </tbody>
</table>
<div style='margin-top: 20px; text-align: right;'>
  <p>Subtotal: {SubtotalFormatted}</p>
  <p style='color: #16a34a;'>Potongan Garansi: {WarrantyDiscountFormatted}</p>
  <p class='total'>Total Akhir: {TotalPayableFormatted}</p>
</div>
<button onclick='window.print()' style='margin-top:20px; padding: 10px 20px; background: #0284c7; color: white; border: none; border-radius: 6px; cursor: pointer;'>🖨️ Cetak / Simpan PDF</button>
</body>
</html>";
        }
    }
}
