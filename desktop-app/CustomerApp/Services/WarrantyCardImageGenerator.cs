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

            // 1. Background: Clean Institutional White
            canvas.Clear(SKColor.Parse("#FFFFFF"));

            // 2. High-Precision Corporate Guilloche / Double Border
            var outerRect = new SKRoundRect(new SKRect(24, 24, width - 24, height - 24), 8, 8);
            using (var outerBorderPaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2.5f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(outerRect, outerBorderPaint);
            }

            var innerRect = new SKRoundRect(new SKRect(30, 30, width - 30, height - 30), 6, 6);
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

            // 3. Header Kop Perusahaan
            var headerRect = new SKRoundRect(new SKRect(48, 48, width - 48, 142), 6, 6);
            using (var headerBg = new SKPaint
            {
                Color = SKColor.Parse("#F8FAFC"),
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
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(headerRect, headerBorder);
            }

            // Brand Badge: PT JTS
            var logoRect = new SKRoundRect(new SKRect(66, 64, 166, 126), 4, 4);
            using (var logoBg = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(logoRect, logoBg);
            }

            using (var logoTextPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = 24,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("PT JTS", 76, 103, logoTextPaint);
            }

            // Company Title & Certificate Subheading
            using (var compTitlePaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                TextSize = 22,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("PT JAYA TEKNOLOGI SOLUSI", 184, 88, compTitlePaint);
            }

            using (var certTitlePaint = new SKPaint
            {
                Color = SKColor.Parse("#334155"),
                TextSize = 13,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("SERTIFIKAT JAMINAN GARANSI RESMI HARDWARE (OFFICIAL WARRANTY)", 184, 108, certTitlePaint);
            }

            using (var certSubPaint = new SKPaint
            {
                Color = SKColor.Parse("#64748B"),
                TextSize = 11,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            })
            {
                canvas.DrawText("Dokumen Sah Perlindungan Servis • Sistem Purna Jual & Jaminan Kualitas Resmi", 184, 126, certSubPaint);
            }

            // Official Verification Status Box (Right Header)
            bool isActive = status.Contains("AKTIF", StringComparison.OrdinalIgnoreCase);
            var statusRect = new SKRoundRect(new SKRect(width - 290, 64, width - 66, 126), 4, 4);
            using (var statusBg = new SKPaint
            {
                Color = SKColor.Parse("#FFFFFF"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(statusRect, statusBg);
            }

            using (var statusBorder = new SKPaint
            {
                Color = SKColor.Parse(isActive ? "#0F172A" : "#94A3B8"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.5f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(statusRect, statusBorder);
            }

            using (var statusHeaderPaint = new SKPaint
            {
                Color = SKColor.Parse("#64748B"),
                TextSize = 9.5f,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("STATUS VERIFIKASI", width - 275, 84, statusHeaderPaint);
            }

            using (var statusValuePaint = new SKPaint
            {
                Color = SKColor.Parse(isActive ? "#0F172A" : "#64748B"),
                TextSize = 13,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                string statusLabel = isActive ? "TERVERIFIKASI AKTIF" : "MASA GARANSI BERAKHIR";
                canvas.DrawText(statusLabel, width - 275, 108, statusValuePaint);
            }

            // 4. Grid of Specifications (Left 2 columns) + QR Code Box (Right column)
            int leftWidth = width - 330;
            int cardW = (leftWidth - 110) / 2;
            int cardH = 142;
            int startX = 48;
            int startY = 162;
            int gap = 14;

            var items = new (string Label, string Value, bool isMono)[]
            {
                ("MODEL PERANGKAT / UNIT", model, false),
                ("NOMOR SERI UNIT (SERIAL NUMBER)", serialNumber, true),
                ("IDENTITAS HARDWARE / BIOS ID", hardwareId, true),
                ("NAMA PEMILIK / PELANGGAN TERDAFTAR", buyerName, false),
                ("PERIODE MASA BERLAKU GARANSI", warrantyEnd, false),
                ("TOKEN STIKER FISIK (FR-07)", qrToken, true)
            };

            for (int i = 0; i < items.Length; i++)
            {
                int col = i % 2;
                int row = i / 2;
                int x = startX + col * (cardW + gap);
                int y = startY + row * (cardH + gap);

                var boxRect = new SKRoundRect(new SKRect(x, y, x + cardW, y + cardH), 6, 6);

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
                    Color = SKColor.Parse("#CBD5E1"),
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 1f,
                    IsAntialias = true
                })
                {
                    canvas.DrawRoundRect(boxRect, boxBorder);
                }

                // Label Header Strip (Clean subtle slate)
                var labelStripRect = new SKRoundRect(new SKRect(x + 1, y + 1, x + cardW - 1, y + 30), 5, 5);
                using (var stripPaint = new SKPaint
                {
                    Color = SKColor.Parse("#F8FAFC"),
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    canvas.DrawRoundRect(labelStripRect, stripPaint);
                }

                // Label Text
                using (var lblPaint = new SKPaint
                {
                    Color = SKColor.Parse("#475569"),
                    TextSize = 10,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
                })
                {
                    canvas.DrawText(items[i].Label, x + 14, y + 20, lblPaint);
                }

                // Divider under label
                using (var divPaint = new SKPaint
                {
                    Color = SKColor.Parse("#E2E8F0"),
                    StrokeWidth = 1f,
                    IsAntialias = true
                })
                {
                    canvas.DrawLine(x, y + 30, x + cardW, y + 30, divPaint);
                }

                // Value Text (Deep Navy / Monochrome)
                using (var valPaint = new SKPaint
                {
                    Color = SKColor.Parse("#0F172A"),
                    TextSize = i == 2 ? 14 : 17,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName(items[i].isMono ? "Consolas" : "Arial", SKFontStyle.Bold)
                })
                {
                    string displayVal = items[i].Value;
                    if (displayVal.Length > 36)
                    {
                        displayVal = displayVal.Substring(0, 33) + "...";
                    }
                    canvas.DrawText(displayVal, x + 14, y + 78, valPaint);
                }

                // Subnote
                using (var subPaint = new SKPaint
                {
                    Color = SKColor.Parse("#64748B"),
                    TextSize = 10.5f,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
                })
                {
                    string subNote = i switch
                    {
                        0 => "Spesifikasi resmi unit terverifikasi distributor",
                        1 => "Nomor unik unit laptop / komputer pelanggan",
                        2 => "Kunci validasi firmware BIOS & Motherboard",
                        3 => "Hak garansi resmi atas nama pemilik terdaftar",
                        4 => "Cakupan servis purna jual, remote & workshop",
                        5 => "Kode token stiker QR fisik casing unit",
                        _ => ""
                    };
                    canvas.DrawText(subNote, x + 14, y + 116, subPaint);
                }
            }

            // 5. Scannable QR Code & Validation Column (Right Side)
            int qrBoxX = width - 290;
            int qrBoxY = startY;
            int qrBoxW = 242;
            int qrBoxH = 454;

            var qrCardRect = new SKRoundRect(new SKRect(qrBoxX, qrBoxY, qrBoxX + qrBoxW, qrBoxY + qrBoxH), 6, 6);
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
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(qrCardRect, qrBorder);
            }

            // QR Header Bar
            var qrHeaderRect = new SKRoundRect(new SKRect(qrBoxX + 1, qrBoxY + 1, qrBoxX + qrBoxW - 1, qrBoxY + 32), 5, 5);
            using (var qrHeaderBg = new SKPaint
            {
                Color = SKColor.Parse("#F8FAFC"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(qrHeaderRect, qrHeaderBg);
            }

            using (var qrTitlePaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                TextSize = 10.5f,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("VALIDASI KEABSAHAN QR", qrBoxX + 34, qrBoxY + 21, qrTitlePaint);
            }

            // Divider under QR title
            using (var divPaint = new SKPaint
            {
                Color = SKColor.Parse("#E2E8F0"),
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawLine(qrBoxX, qrBoxY + 32, qrBoxX + qrBoxW, qrBoxY + 32, divPaint);
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
                    int qrDrawSize = 175;
                    int qrX = qrBoxX + (qrBoxW - qrDrawSize) / 2;
                    int qrY = qrBoxY + 48;
                    var destRect = new SKRect(qrX, qrY, qrX + qrDrawSize, qrY + qrDrawSize);
                    canvas.DrawBitmap(qrBmp, destRect);
                }
            }
            catch
            {
                using var fallbackPaint = new SKPaint { Color = SKColor.Parse("#0F172A"), Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f };
                canvas.DrawRect(new SKRect(qrBoxX + 30, qrBoxY + 48, qrBoxX + 212, qrBoxY + 230), fallbackPaint);
            }

            // Token Text under QR
            using (var tokenLabelPaint = new SKPaint
            {
                Color = SKColor.Parse("#64748B"),
                TextSize = 9.5f,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("KODE TOKEN KEABSAHAN:", qrBoxX + 44, qrBoxY + 252, tokenLabelPaint);
            }

            using (var tokenValPaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                TextSize = 12,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Consolas", SKFontStyle.Bold)
            })
            {
                canvas.DrawText(qrToken, qrBoxX + 42, qrBoxY + 276, tokenValPaint);
            }

            // Security seal note under QR
            using (var secureBoxBg = new SKPaint
            {
                Color = SKColor.Parse("#F8FAFC"),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(qrBoxX + 16, qrBoxY + 304, qrBoxX + qrBoxW - 16, qrBoxY + qrBoxH - 16), 4, 4), secureBoxBg);
            }

            using (var secureBoxBorder = new SKPaint
            {
                Color = SKColor.Parse("#E2E8F0"),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(qrBoxX + 16, qrBoxY + 304, qrBoxX + qrBoxW - 16, qrBoxY + qrBoxH - 16), 4, 4), secureBoxBorder);
            }

            using (var secureTitlePaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                TextSize = 9.5f,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("AUTENTIKASI ELEKTRONIK", qrBoxX + 32, qrBoxY + 328, secureTitlePaint);
            }

            using (var secureNotePaint = new SKPaint
            {
                Color = SKColor.Parse("#475569"),
                TextSize = 9.5f,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            })
            {
                canvas.DrawText("• Terdaftar di Database Cloud PT JTS", qrBoxX + 32, qrBoxY + 354, secureNotePaint);
                canvas.DrawText("• Integritas Dokumen SHA-256", qrBoxX + 32, qrBoxY + 378, secureNotePaint);
                canvas.DrawText("• Jaminan Resmi Suku Cadang OEM", qrBoxX + 32, qrBoxY + 402, secureNotePaint);
                canvas.DrawText("• Sah untuk Klaim Seluruh Layanan", qrBoxX + 32, qrBoxY + 426, secureNotePaint);
            }

            // 6. Footer Section (Corporate formal line & metadata)
            int footerY = height - 68;
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
                TextSize = 11,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            })
            {
                canvas.DrawText("PT Jaya Teknologi Solusi • Sentra Layanan Purna Jual & Garansi Resmi Hardware • Hotline: (021) 5088-7799 • aftersales@jts.co.id", 48, footerY + 32, footerLeft);
            }

            using (var sealPaint = new SKPaint
            {
                Color = SKColor.Parse("#0F172A"),
                TextSize = 11.5f,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            })
            {
                canvas.DrawText("OFFICIAL HARDWARE WARRANTY CERTIFICATE", width - 340, footerY + 32, sealPaint);
            }

            // 7. Save image to Downloads folder
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
