using System;
using System.IO;
using QRCoder;
using SkiaSharp;

namespace CustomerApp.Services
{
    public static class WarrantyCardImageGenerator
    {
        public static string GenerateAndSaveCardImage(
            string model,
            string serialNumber,
            string hardwareId,
            string buyerName,
            string warrantyEnd,
            string purchaseDate,
            string status,
            string qrToken)
        {
            const int width = 1200;
            const int height = 750;

            using var bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
            using var canvas = new SKCanvas(bitmap);

            // 1. Background: Clean Crisp Off-White / Ivory Certificate Tone
            canvas.Clear(SKColor.Parse("#F8FAFC"));

            // Decorative inner border frame (Double Gold/Teal Luxury Certificate Border)
            var outerRect = new SKRoundRect(new SKRect(24, 24, width - 24, height - 24), 16, 16);
            using (var outerBorderPaint = new SKPaint
            {
                Color = SKColor.Parse("#2563EB"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(outerRect, outerBorderPaint);
            }

            var innerRect = new SKRoundRect(new SKRect(32, 32, width - 32, height - 32), 12, 12);
            using (var innerBorderPaint = new SKPaint
            {
                Color = SKColor.Parse("#CBD5E1"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(innerRect, innerBorderPaint);
            }

            // 2. Header Section (Clean White with Navy & Emerald accents)
            var headerRect = new SKRoundRect(new SKRect(48, 48, width - 48, 145), 10, 10);
            using (var headerBg = new SKPaint
            {
                Color = SKColor.Parse("#FFFFFF"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(headerRect, headerBg);
            }

            using (var headerBorder = new SKPaint
            {
                Color = SKColor.Parse("#E2E8F0"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(headerRect, headerBorder);
            }

            // Brand Logo Pill
            var logoRect = new SKRoundRect(new SKRect(68, 64, 185, 128), 8, 8);
            using (var logoBg = new SKPaint
            {
                Color = SKColor.Parse("#2563EB"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(logoRect, logoBg);
            }

            using (var logoTextPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 28,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("PT JTS", 80, 106, logoTextPaint);
            }

            // Header Titles (Dark Navy)
            using (var titlePaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                TextSize = 26,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("SERTIFIKAT JAMINAN GARANSI RESMI", 205, 90, titlePaint);
            }

            using (var subTitlePaint = new SKPaint
            {
                Color = SKColor.Parse("#64748B"),
                TextSize = 13,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            })
            {
                canvas.DrawText("Dokumen Digital Sah • PT Jaya Teknologi Solusi Support System", 205, 118, subTitlePaint);
            }

            // Status Badge (Right Header)
            bool isActive = status.Contains("AKTIF", StringComparison.OrdinalIgnoreCase);
            var statusRect = new SKRoundRect(new SKRect(width - 320, 68, width - 68, 126), 8, 8);
            using (var statusBg = new SKPaint
            {
                Color = isActive ? SKColor.Parse("#ECFDF5") : SKColor.Parse("#FEF2F2"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(statusRect, statusBg);
            }

            using (var statusBorder = new SKPaint
            {
                Color = isActive ? SKColor.Parse("#10B981") : SKColor.Parse("#EF4444"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(statusRect, statusBorder);
            }

            using (var statusTextPaint = new SKPaint
            {
                Color = isActive ? SKColor.Parse("#047857") : SKColor.Parse("#B91C1C"),
                TextSize = 14,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                string statusLabel = isActive ? "✓ GARANSI RESMI AKTIF" : "✕ GARANSI BERAKHIR";
                canvas.DrawText(statusLabel, width - 295, 102, statusTextPaint);
            }

            // 3. Grid of Specifications (Left 2 columns) + QR Code Box (Right column)
            int leftWidth = width - 330; // 870px for specs
            int cardW = (leftWidth - 110) / 2;
            int cardH = 140;
            int startX = 48;
            int startY = 165;
            int gap = 16;

            var items = new (string Label, string Value, string ColorHex)[]
            {
                ("MODEL PERANGKAT", model, "#0F172A"),
                ("NOMOR SERI UNIT (S/N)", serialNumber, "#2563EB"),
                ("IDENTITAS HARDWARE / BIOS ID", hardwareId, "#334155"),
                ("NAMA PEMILIK / PELANGGAN", buyerName, "#0F172A"),
                ("MASA BERLAKU GARANSI", warrantyEnd, "#059669"),
                ("TOKEN STIKER FISIK (FR-07)", qrToken, "#D97706")
            };

            for (int i = 0; i < items.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int x = startX + col * (cardW + gap);
                int y = startY + row * (cardH + gap);

                var boxRect = new SKRoundRect(new SKRect(x, y, x + cardW, y + cardH), 8, 8);

                // Card Background
                using (var boxBg = new SKPaint
                {
                    Color = SKColor.Parse("#FFFFFF"),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    canvas.DrawRoundRect(boxRect, boxBg);
                }

                // Card Border
                using (var boxBorder = new SKPaint
                {
                    Color = SKColor.Parse("#E2E8F0"),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f,
                    IsAntialias = true
                })
                {
                    canvas.DrawRoundRect(boxRect, boxBorder);
                }

                // Card Top Accent Stripe
                using (var stripePaint = new SKPaint
                {
                    Color = SKColor.Parse(items[i].ColorHex),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    canvas.DrawRoundRect(new SKRoundRect(new SKRect(x, y, x + cardW, y + 4), 2, 2), stripePaint);
                }

                // Label
                using (var lblPaint = new SKPaint
                {
                    Color = SKColor.Parse("#64748B"),
                    TextSize = 11,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText(items[i].Label, x + 16, y + 32, lblPaint);
                }

                // Value
                using (var valPaint = new SKPaint
                {
                    Color = SKColor.Parse(items[i].ColorHex),
                    TextSize = i == 2 ? 15 : 19,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName(i == 1 || i == 2 || i == 5 ? "Consolas" : "Arial", SKFontStyle.Bold)
                })
                {
                    string displayVal = items[i].Value;
                    if (displayVal.Length > 34)
                    {
                        displayVal = displayVal.Substring(0, 31) + "...";
                    }
                    canvas.DrawText(displayVal, x + 16, y + 74, valPaint);
                }

                // Subnote
                using (var subPaint = new SKPaint
                {
                    Color = SKColor.Parse("#94A3B8"),
                    TextSize = 11,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                })
                {
                    string subNote = i switch
                    {
                        0 => "Spesifikasi resmi terverifikasi pabrikan",
                        1 => "Nomor unik unit laptop pelanggan",
                        2 => "Kunci verifikasi BIOS & Motherboard",
                        3 => "Hak kepemilikan garansi resmi",
                        4 => "Perlindungan servis remote & on-site",
                        5 => "Token validasi fisik QR Scanner",
                        _ => ""
                    };
                    canvas.DrawText(subNote, x + 16, y + 112, subPaint);
                }
            }

            // 4. Genuine Scannable QR Code Box (Right Side)
            int qrBoxX = width - 290;
            int qrBoxY = startY;
            int qrBoxW = 242;
            int qrBoxH = 452;

            var qrCardRect = new SKRoundRect(new SKRect(qrBoxX, qrBoxY, qrBoxX + qrBoxW, qrBoxY + qrBoxH), 10, 10);
            using (var qrBg = new SKPaint
            {
                Color = SKColor.Parse("#FFFFFF"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(qrCardRect, qrBg);
            }

            using (var qrBorder = new SKPaint
            {
                Color = SKColor.Parse("#CBD5E1"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(qrCardRect, qrBorder);
            }

            // QR Header Text
            using (var qrTitlePaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                TextSize = 12,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("PINDAI UNTUK VERIFIKASI", qrBoxX + 24, qrBoxY + 36, qrTitlePaint);
            }

            using (var qrSubPaint = new SKPaint
            {
                Color = SKColor.Parse("#64748B"),
                TextSize = 10,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            })
            {
                canvas.DrawText("Validasi Keaslian Online PT JTS", qrBoxX + 28, qrBoxY + 54, qrSubPaint);
            }

            // Generate Real QR Code using QRCoder
            string cleanSerial = string.IsNullOrWhiteSpace(serialNumber) ? "UNIT" : serialNumber.Trim();
            string verificationUrl = $"http://127.0.0.1:8000/qr/lookup/{qrToken}";

            try
            {
                using var qrGenerator = new QRCodeGenerator();
                using var qrCodeData = qrGenerator.CreateQrCode(verificationUrl, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                byte[] qrBytes = qrCode.GetGraphic(8, new byte[] { 15, 23, 42 }, new byte[] { 255, 255, 255 });

                using var qrBmp = SKBitmap.Decode(qrBytes);
                if (qrBmp != null)
                {
                    int qrDrawSize = 180;
                    int qrX = qrBoxX + (qrBoxW - qrDrawSize) / 2;
                    int qrY = qrBoxY + 70;
                    var destRect = new SKRect(qrX, qrY, qrX + qrDrawSize, qrY + qrDrawSize);
                    canvas.DrawBitmap(qrBmp, destRect);
                }
            }
            catch
            {
                // Fallback box if QR generation fails
                using var fallbackPaint = new SKPaint { Color = SKColor.Parse("#2563EB"), Style = SKPaintStyle.Stroke, StrokeWidth = 2 };
                canvas.DrawRect(new SKRect(qrBoxX + 30, qrBoxY + 70, qrBoxX + 210, qrBoxY + 250), fallbackPaint);
            }

            // Token Text under QR
            using (var tokenLabelPaint = new SKPaint
            {
                Color = SKColor.Parse("#64748B"),
                TextSize = 10,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("KODE TOKEN KEABSAHAN:", qrBoxX + 44, qrBoxY + 285, tokenLabelPaint);
            }

            using (var tokenValPaint = new SKPaint
            {
                Color = SKColor.Parse("#2563EB"),
                TextSize = 13,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold)
            })
            {
                canvas.DrawText(qrToken, qrBoxX + 50, qrBoxY + 310, tokenValPaint);
            }

            // Security seal note under QR
            using (var secureBoxBg = new SKPaint
            {
                Color = SKColor.Parse("#F1F5F9"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(qrBoxX + 16, qrBoxY + 335, qrBoxX + qrBoxW - 16, qrBoxY + qrBoxH - 16), 6, 6), secureBoxBg);
            }

            using (var secureNotePaint = new SKPaint
            {
                Color = SKColor.Parse("#334155"),
                TextSize = 10,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            })
            {
                canvas.DrawText("✓ Terhubung ke database", qrBoxX + 32, qrBoxY + 365, secureNotePaint);
                canvas.DrawText("✓ Terenkripsi SHA-256", qrBoxX + 32, qrBoxY + 388, secureNotePaint);
                canvas.DrawText("✓ Garansi Resmi PT JTS", qrBoxX + 32, qrBoxY + 411, secureNotePaint);
            }

            // 5. Footer Section (Clean light border & official seal)
            int footerY = height - 75;
            using (var linePaint = new SKPaint
            {
                Color = SKColor.Parse("#CBD5E1"),
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawLine(48, footerY, width - 48, footerY, linePaint);
            }

            using (var footerLeft = new SKPaint
            {
                Color = SKColor.Parse("#64748B"),
                TextSize = 12,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            })
            {
                canvas.DrawText("PT Jaya Teknologi Solusi • Layanan Garansi & Servis Resmi • Hotline: 0812-9900-1122 • support@jts.co.id", 48, footerY + 36, footerLeft);
            }

            using (var sealPaint = new SKPaint
            {
                Color = SKColor.Parse("#2563EB"),
                TextSize = 13,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("★ JTS VERIFIED OFFICIAL WARRANTY ★", width - 365, footerY + 36, sealPaint);
            }

            // 6. Save image to Downloads folder
            string downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(downloadsDir))
            {
                downloadsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            }
            if (!Directory.Exists(downloadsDir))
            {
                downloadsDir = Path.GetTempPath();
            }

            string cleanSerialFileName = cleanSerial.Replace("/", "_").Replace("\\", "_");
            string outputFilePath = Path.Combine(downloadsDir, $"Kartu_Garansi_JTS_{cleanSerialFileName}.png");

            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.Open(outputFilePath, FileMode.Create, FileAccess.Write))
            {
                data.SaveTo(stream);
            }

            return outputFilePath;
        }
    }
}
