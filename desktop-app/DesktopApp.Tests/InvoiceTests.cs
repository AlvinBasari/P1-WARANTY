using System.Collections.Generic;
using CustomerApp.ViewModels;
using SharedCore.Models;
using SharedCore.Services;
using Xunit;

namespace DesktopApp.Tests
{
    public class InvoiceTests
    {
        [Fact]
        public void InvoiceViewModel_LoadsFullWarrantyCoverage_CalculatesZeroTotal()
        {
            var apiClient = new ApiClient("http://localhost:8000");
            bool closed = false;
            var vm = new InvoiceViewModel(apiClient, () => closed = true);

            var dto = new InvoiceDto
            {
                Id = 10,
                InvoiceNumber = "INV-202609-1001",
                IssueDate = "2026-09-07 15:00:00",
                SubtotalAmount = "2500000",
                WarrantyDiscountAmount = "2500000",
                TotalPayableAmount = "0",
                PaymentStatus = "paid_by_warranty",
                Items = new List<InvoiceItemDto>
                {
                    new()
                    {
                        ItemName = "Penggantian Motherboard",
                        ItemCode = "MB-T14-01",
                        Category = "sparepart",
                        Quantity = 1,
                        UnitPrice = "2250000",
                        Subtotal = "2250000",
                        IsCoveredByWarranty = true,
                        WarrantyCoverageAmount = "2250000",
                        CustomerPayableAmount = "0"
                    },
                    new()
                    {
                        ItemName = "Jasa Pemasangan & Kalibrasi",
                        Category = "service_fee",
                        Quantity = 1,
                        UnitPrice = "250000",
                        Subtotal = "250000",
                        IsCoveredByWarranty = true,
                        WarrantyCoverageAmount = "250000",
                        CustomerPayableAmount = "0"
                    }
                }
            };

            vm.LoadFromDto(dto);

            Assert.Equal("INV-202609-1001", vm.InvoiceNumber);
            Assert.Equal("Rp 2,500,000", vm.SubtotalFormatted);
            Assert.Equal("- Rp 2,500,000", vm.WarrantyDiscountFormatted);
            Assert.Equal("Rp 0", vm.TotalPayableFormatted);
            Assert.True(vm.IsFullyCoveredByWarranty);
            Assert.Contains("LUNAS", vm.PaymentStatusBadge);
            Assert.Equal(2, vm.DisplayItems.Count);
            Assert.True(vm.DisplayItems[0].IsCoveredByWarranty);
            Assert.Equal("Rp 0 (Covered)", vm.DisplayItems[0].CustomerPayableFormatted);

            vm.CloseCommand.Execute(null);
            Assert.True(closed);
        }

        [Fact]
        public void InvoiceViewModel_LoadsPartialWarrantyCoverage_CalculatesClientPayable()
        {
            var apiClient = new ApiClient("http://localhost:8000");
            var vm = new InvoiceViewModel(apiClient, () => { });

            var dto = new InvoiceDto
            {
                Id = 11,
                InvoiceNumber = "INV-202609-1002",
                IssueDate = "2026-09-07 15:00:00",
                SubtotalAmount = "1500000",
                WarrantyDiscountAmount = "1000000",
                TotalPayableAmount = "500000",
                PaymentStatus = "unpaid",
                Items = new List<InvoiceItemDto>
                {
                    new()
                    {
                        ItemName = "Ganti Keyboard (Garansi)",
                        Category = "sparepart",
                        Quantity = 1,
                        UnitPrice = "1000000",
                        Subtotal = "1000000",
                        IsCoveredByWarranty = true,
                        WarrantyCoverageAmount = "1000000",
                        CustomerPayableAmount = "0"
                    },
                    new()
                    {
                        ItemName = "Upgrade SSD NVMe 1TB (Non-Garansi)",
                        Category = "sparepart",
                        Quantity = 1,
                        UnitPrice = "500000",
                        Subtotal = "500000",
                        IsCoveredByWarranty = false,
                        WarrantyCoverageAmount = "0",
                        CustomerPayableAmount = "500000"
                    }
                }
            };

            vm.LoadFromDto(dto);

            Assert.Equal("Rp 1,500,000", vm.SubtotalFormatted);
            Assert.Equal("- Rp 1,000,000", vm.WarrantyDiscountFormatted);
            Assert.Equal("Rp 500,000", vm.TotalPayableFormatted);
            Assert.False(vm.IsFullyCoveredByWarranty);
            Assert.Contains("MENUNGGU PEMBAYARAN", vm.PaymentStatusBadge);
            Assert.Equal(2, vm.DisplayItems.Count);
            Assert.False(vm.DisplayItems[1].IsCoveredByWarranty);
            Assert.Equal("Rp 500,000", vm.DisplayItems[1].CustomerPayableFormatted);
        }
    }
}
