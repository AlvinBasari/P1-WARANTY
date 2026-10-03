using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SharedCore.Models;
using SharedCore.Services;

namespace TechnicianApp.ViewModels
{
    public partial class InvoiceManagerViewModel : ViewModelBase
    {
        private readonly ApiClient _apiClient;
        private readonly Action _onClose;

        [ObservableProperty]
        private RepairRequestDto _ticket;

        [ObservableProperty]
        private InvoiceDto? _currentInvoice;

        [ObservableProperty]
        private string _invoiceNumber = "DRAF INVOICE";

        [ObservableProperty]
        private string _issueDateText = DateTime.Now.ToString("dd MMM yyyy");

        [ObservableProperty]
        private string _notes = "Faktur resmi jaminan garansi dan perbaikan perangkat PT JTS.";

        [ObservableProperty]
        private string _termsAndConditions = "1. Suku cadang resmi digaransi selama 1 tahun.\n2. Biaya yang dijamin garansi resmi telah dipotong 100%.\n3. Harap simpan faktur ini sebagai bukti klaim sah.";

        [ObservableProperty]
        private string _paymentStatus = "paid_by_warranty";

        [ObservableProperty]
        private string _paymentMethod = "warranty_claim";

        [ObservableProperty]
        private ObservableCollection<InvoiceItemPayloadDto> _items = new();

        // New Item Form
        [ObservableProperty]
        private string _newItemName = string.Empty;

        [ObservableProperty]
        private string _newItemCode = string.Empty;

        [ObservableProperty]
        private string _newItemCategory = "sparepart"; // sparepart, service_fee, diagnostic_fee, transport_fee, other

        [ObservableProperty]
        private int _newItemQuantity = 1;

        [ObservableProperty]
        private decimal _newItemUnitPrice = 150000;

        [ObservableProperty]
        private bool _newItemIsCovered = true;

        [ObservableProperty]
        private string _newItemNotes = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string? _statusMessage;

        [ObservableProperty]
        private decimal _subtotalAmount;

        [ObservableProperty]
        private decimal _warrantyDiscountAmount;

        [ObservableProperty]
        private decimal _totalPayableAmount;

        public InvoiceManagerViewModel(ApiClient apiClient, RepairRequestDto ticket, Action onClose)
        {
            _apiClient = apiClient;
            _ticket = ticket;
            _onClose = onClose;

            _ = LoadInvoiceAsync();
        }

        public void RecalculateTotals()
        {
            decimal sub = 0;
            decimal discount = 0;

            foreach (var itm in Items)
            {
                var lineTotal = itm.Quantity * itm.UnitPrice;
                sub += lineTotal;
                if (itm.IsCoveredByWarranty)
                {
                    discount += lineTotal;
                }
            }

            SubtotalAmount = sub;
            WarrantyDiscountAmount = discount;
            TotalPayableAmount = Math.Max(0, sub - discount);

            if (TotalPayableAmount == 0 && Items.Count > 0)
            {
                PaymentStatus = "paid_by_warranty";
            }
            else if (TotalPayableAmount > 0)
            {
                PaymentStatus = "unpaid";
            }
        }

        [RelayCommand]
        public async Task LoadInvoiceAsync()
        {
            IsLoading = true;
            StatusMessage = null;

            try
            {
                var res = await _apiClient.GetInvoiceByRepairRequestIdAsync(Ticket.Id);
                if (res.HasInvoice && res.Invoice != null)
                {
                    CurrentInvoice = res.Invoice;
                    InvoiceNumber = res.Invoice.InvoiceNumber;
                    IssueDateText = res.Invoice.IssueDate ?? DateTime.Now.ToString("dd MMM yyyy");
                    Notes = res.Invoice.Notes ?? Notes;
                    TermsAndConditions = res.Invoice.TermsAndConditions ?? TermsAndConditions;
                    PaymentStatus = res.Invoice.PaymentStatus;
                    PaymentMethod = res.Invoice.PaymentMethod ?? PaymentMethod;

                    Items.Clear();
                    if (res.Invoice.Items != null)
                    {
                        foreach (var itm in res.Invoice.Items)
                        {
                            decimal.TryParse(itm.UnitPrice, out var unitPrice);
                            Items.Add(new InvoiceItemPayloadDto
                            {
                                ItemName = itm.ItemName,
                                ItemCode = itm.ItemCode,
                                Category = itm.Category,
                                Quantity = itm.Quantity,
                                UnitPrice = unitPrice,
                                IsCoveredByWarranty = itm.IsCoveredByWarranty,
                                Notes = itm.Notes
                            });
                        }
                    }
                }
                else
                {
                    // Pre-populate default inspection fee if new
                    if (Items.Count == 0)
                    {
                        Items.Add(new InvoiceItemPayloadDto
                        {
                            ItemName = "Jasa Diagnosa & Perbaikan Hardware",
                            Category = "service_fee",
                            Quantity = 1,
                            UnitPrice = 250000,
                            IsCoveredByWarranty = true,
                            Notes = "Dijamin penuh oleh garansi aktif PT JTS"
                        });
                    }
                }

                RecalculateTotals();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal memuat invoice: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void AddItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemName))
            {
                StatusMessage = "Nama item / suku cadang tidak boleh kosong.";
                return;
            }

            if (NewItemQuantity <= 0)
            {
                NewItemQuantity = 1;
            }

            Items.Add(new InvoiceItemPayloadDto
            {
                ItemName = NewItemName.Trim(),
                ItemCode = string.IsNullOrWhiteSpace(NewItemCode) ? null : NewItemCode.Trim(),
                Category = NewItemCategory,
                Quantity = NewItemQuantity,
                UnitPrice = NewItemUnitPrice,
                IsCoveredByWarranty = NewItemIsCovered,
                Notes = string.IsNullOrWhiteSpace(NewItemNotes) ? null : NewItemNotes.Trim()
            });

            // Reset Form
            NewItemName = string.Empty;
            NewItemCode = string.Empty;
            NewItemQuantity = 1;
            NewItemUnitPrice = 150000;
            NewItemIsCovered = true;
            NewItemNotes = string.Empty;

            RecalculateTotals();
            StatusMessage = "✓ Item berhasil ditambahkan ke daftar faktur.";
        }

        [RelayCommand]
        public void RemoveItem(InvoiceItemPayloadDto item)
        {
            if (Items.Contains(item))
            {
                Items.Remove(item);
                RecalculateTotals();
                StatusMessage = "Item dihapus dari faktur.";
            }
        }

        [RelayCommand]
        public async Task SaveInvoiceAsync()
        {
            if (Items.Count == 0)
            {
                StatusMessage = "Tambahkan minimal 1 item sebelum menyimpan faktur.";
                return;
            }

            IsLoading = true;
            StatusMessage = null;

            try
            {
                var payload = new CreateInvoicePayloadDto
                {
                    Notes = Notes,
                    TermsAndConditions = TermsAndConditions,
                    PaymentStatus = PaymentStatus,
                    PaymentMethod = PaymentMethod,
                    Items = Items.ToList()
                };

                var saved = await _apiClient.SaveInvoiceAsync(Ticket.Id, payload);
                CurrentInvoice = saved;
                InvoiceNumber = saved.InvoiceNumber;
                StatusMessage = "✓ Faktur perbaikan berhasil diterbitkan & disinkronkan!";
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
        public async Task PrintOrDownloadHtmlAsync()
        {
            if (CurrentInvoice == null || CurrentInvoice.Id == 0)
            {
                StatusMessage = "Simpan faktur terlebih dahulu sebelum mencetak.";
                return;
            }

            IsLoading = true;
            StatusMessage = null;

            try
            {
                string html = await _apiClient.DownloadInvoiceHtmlAsync(CurrentInvoice.Id);
                string tempPath = Path.Combine(Path.GetTempPath(), $"Invoice_{CurrentInvoice.InvoiceNumber}.html");
                await File.WriteAllTextAsync(tempPath, html);

                // Open in default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPath,
                    UseShellExecute = true
                });

                StatusMessage = $"✓ Faktur dibuka di browser: {Path.GetFileName(tempPath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal membuka faktur: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public void Close()
        {
            _onClose();
        }
    }
}
