using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SharedCore.Services
{
    public enum DeviceCategory
    {
        DesktopTower,
        DesktopSff,
        MiniPc,
        Laptop,
        AllInOne,
        GenericPc
    }

    public static class HardwareIntelligenceEngine
    {
        /// <summary>
        /// Mendeteksi kategori form-factor perangkat secara dinamis (Laptop, Desktop Tower, Desktop SFF, Mini PC, All-in-One).
        /// </summary>
        public static DeviceCategory DetectDeviceCategory(string manufacturer, string modelName, bool hasBattery = false)
        {
            manufacturer = (manufacturer ?? "").Trim().ToUpperInvariant();
            modelName = (modelName ?? "").Trim().ToUpperInvariant();

            // 1. Cek baterai terintegrasi (pasti Laptop/Notebook jika ada baterai Li-Ion)
            if (hasBattery)
            {
                return DeviceCategory.Laptop;
            }

            // 2. Deteksi berdasarkan pola kata kunci model perangkat (akurasi tertinggi)
            if (modelName.Contains("THINKPAD") || modelName.Contains("VIVOBOOK") || modelName.Contains("ZENBOOK") ||
                modelName.Contains("IDEAPAD") || modelName.Contains("PAVILION") || modelName.Contains("INSPIRON") ||
                modelName.Contains("LATITUDE") || modelName.Contains("VOSTRO") || modelName.Contains("MACBOOK") ||
                modelName.Contains("NOTEBOOK") || modelName.Contains("LAPTOP") || modelName.Contains("SURFACE") ||
                modelName.Contains("LEGION") || modelName.Contains("LOQ") || modelName.Contains("PREDATOR") ||
                modelName.Contains("NITRO") || modelName.Contains("VICTUS") || modelName.Contains("OMEN") ||
                modelName.Contains("TUF") || modelName.Contains("ROG") || modelName.Contains("ALIENWARE") ||
                modelName.Contains("KATANA") || modelName.Contains("SWIFT") || modelName.Contains("BLADE"))
            {
                return DeviceCategory.Laptop;
            }

            if (modelName.Contains("SFF") || modelName.Contains("M710S") || modelName.Contains("M720S") ||
                modelName.Contains("M910S") || modelName.Contains("M920S") || modelName.Contains("OPTIPLEX 30") ||
                modelName.Contains("OPTIPLEX 70") || modelName.Contains("PRODESK 400") || modelName.Contains("PRODESK 600") ||
                modelName.Contains("ELITEDESK 800") || modelName.Contains("SLIM"))
            {
                return DeviceCategory.DesktopSff;
            }

            if (modelName.Contains("TINY") || modelName.Contains("MICRO") || modelName.Contains("NUC") ||
                modelName.Contains("MINI PC") || modelName.Contains("DESKMINI") || modelName.Contains("BRIX"))
            {
                return DeviceCategory.MiniPc;
            }

            if (modelName.Contains("ALL-IN-ONE") || modelName.Contains("AIO") || modelName.Contains("IMAC") ||
                modelName.Contains("IDEACENTRE AIO") || modelName.Contains("PAVILION ALL-IN-ONE"))
            {
                return DeviceCategory.AllInOne;
            }

            // 3. Fallback ke sysfs dmi chassis_type pada Linux jika model generik
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/sys/class/dmi/id/chassis_type"))
                {
                    if (int.TryParse(File.ReadAllText("/sys/class/dmi/id/chassis_type").Trim(), out var chassis))
                    {
                        // 8: Portable, 9: Laptop, 10: Notebook, 11: Handheld, 14: Sub-Notebook, 30: Tablet, 31: Convertible, 32: Detachable
                        if (chassis is 8 or 9 or 10 or 11 or 14 or 30 or 31 or 32)
                            return DeviceCategory.Laptop;

                        // 4: Low Profile Desktop, 15: Space-saving, 16: Lunch Box
                        if (chassis is 4 or 15 or 16)
                            return DeviceCategory.DesktopSff;

                        // 13: All in One
                        if (chassis == 13)
                            return DeviceCategory.AllInOne;

                        // 34: Mini PC, 35: Stick PC
                        if (chassis is 34 or 35)
                            return DeviceCategory.MiniPc;

                        // 3: Desktop, 6: Mini Tower, 7: Tower, 24: Sealed-case PC
                        if (chassis is 3 or 6 or 7 or 24)
                            return DeviceCategory.DesktopTower;
                    }
                }
            }
            catch { }

            return DeviceCategory.DesktopTower;
        }

        /// <summary>
        /// Menganalisis CPU dan menghasilkan Soket, Chipset, TDP, iGPU, serta panduan upgrade yang ramah konsumen.
        /// </summary>
        public static (string Socket, string Chipset, string Tdp, string Igpu, string Recommendation, string Feasibility)
            AnalyzeCpu(string cpuName, string vendor, int cores, int threads, DeviceCategory category)
        {
            string name = (cpuName ?? "").Trim();
            string upper = name.ToUpperInvariant();

            bool isMobileCpu = category == DeviceCategory.Laptop ||
                               Regex.IsMatch(upper, @"\b\d{4,5}[UHYM]\b") ||
                               Regex.IsMatch(upper, @"\b\d{4,5}(G[1-7]|HS|HX|HK)\b") ||
                               upper.Contains("MOBILE") || upper.Contains("CELERON N") || upper.Contains("PENTIUM SILVER");

            // ==========================================
            // 1. INTEL PROCESSORS
            // ==========================================
            if (upper.Contains("INTEL") || vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            {
                // Laptop / Mobile Intel CPU
                if (isMobileCpu)
                {
                    string igpu = "Intel(R) Iris Xe / UHD Graphics";
                    if (upper.Contains("ULTRA")) igpu = "Intel(R) Arc Graphics & NPU AI";
                    else if (upper.Contains("HD GRAPHICS") || upper.Contains("I3-6") || upper.Contains("I3-7") || upper.Contains("I5-6") || upper.Contains("I5-7"))
                        igpu = "Intel(R) HD Graphics 520 / 620";

                    return (
                        "BGA (Tersolder Terintegrasi)",
                        "Intel Integrated Mobile Chipset",
                        "15W - 45W (Hemat Daya)",
                        igpu,
                        "Prosesor laptop ini terpasang permanen pada motherboard. Untuk mempercepat kinerja laptop Anda, kami sangat menyarankan upgrade kapasitas RAM dan media penyimpanan SSD M.2 NVMe.",
                        "Tersolder (Fokus Upgrade RAM & SSD)"
                    );
                }

                // Desktop Intel Core Ultra Series
                if (upper.Contains("ULTRA"))
                {
                    return (
                        "Socket LGA1851 (Intel Platform)",
                        "Intel Series 800 Chipset",
                        "65W - 125W (Performa Tinggi)",
                        "Intel(R) Arc Graphics",
                        "Prosesor generasi terbaru dengan kemampuan komputasi tinggi, sangat bertenaga untuk software modern dan multitasking berat.",
                        "Kinerja Sangat Optimal"
                    );
                }

                // Desktop Intel 12th / 13th / 14th Gen
                if (Regex.IsMatch(upper, @"I[3579]-(12|13|14)\d{3}") || upper.Contains("12TH GEN") || upper.Contains("13TH GEN") || upper.Contains("14TH GEN"))
                {
                    string rec = "Prosesor dapat ditingkatkan ke seri Intel Core i7 atau Core i9 pada soket LGA1700 yang sama untuk kebutuhan komputasi dan kreasi konten tingkat tinggi.";
                    if (upper.Contains("I7-") || upper.Contains("I9-"))
                        rec = "Prosesor kelas atas dengan kinerja multi-core yang sangat memadai untuk gaming, editing video, dan software profesional.";

                    return (
                        "Socket LGA1700",
                        "Intel 600/700 Series",
                        upper.Contains("K") ? "125W - 253W" : "65W (Standar Desktop)",
                        "Intel(R) UHD Graphics 730/770",
                        rec,
                        "Dapat Di-upgrade (Socket Modular)"
                    );
                }

                // Desktop Intel 10th / 11th Gen
                if (Regex.IsMatch(upper, @"I[3579]-(10|11)\d{3}") || upper.Contains("10TH GEN") || upper.Contains("11TH GEN"))
                {
                    string rec = "Dapat ditingkatkan ke Intel Core i5-11400 atau Core i7-11700 (8 Core / 16 Thread) untuk mempercepat multitasking dan performa aplikasi berat.";
                    if (upper.Contains("I7-") || upper.Contains("I9-"))
                        rec = "Prosesor berkinerja tinggi, sudah sangat nyaman dan responsif untuk berbagai kebutuhan pekerjaan.";

                    return (
                        "Socket LGA1200",
                        "Intel 400/500 Series",
                        "65W (Standar Desktop)",
                        "Intel(R) UHD Graphics",
                        rec,
                        "Dapat Di-upgrade (Socket Modular)"
                    );
                }

                // Desktop Intel 8th / 9th Gen
                if (Regex.IsMatch(upper, @"I[3579]-[89]\d{3}"))
                {
                    string rec = "Dapat ditingkatkan ke Intel Core i5-9400 (6 Core) atau Core i7-9700 (8 Core) untuk peningkatan kecepatan multitasking.";
                    if (upper.Contains("I7-") || upper.Contains("I9-"))
                        rec = "Prosesor 8 Core berkinerja stabil dan nyaman untuk aktivitas harian maupun kantor.";

                    return (
                        "Socket LGA1151-v2",
                        "Intel 300 Series",
                        "65W (Standar Desktop)",
                        "Intel(R) UHD Graphics 630",
                        rec,
                        "Dapat Di-upgrade (Socket Modular)"
                    );
                }

                // Desktop Intel 6th / 7th Gen (Skylake / Kaby Lake)
                if (Regex.IsMatch(upper, @"I[3579]-[67]\d{3}") || upper.Contains("7100") || upper.Contains("6100") || upper.Contains("7400") || upper.Contains("7500") || upper.Contains("7700") || upper.Contains("G4560") || upper.Contains("G4400") || upper.Contains("G3900") || upper.Contains("G3930"))
                {
                    string rec = "Untuk mempercepat proses multitasking dan membuka banyak aplikasi berat bersamaan, prosesor dapat di-upgrade ke Intel Core i5-7400/7500 (4 Core) atau Core i7-7700 (4 Core / 8 Thread).";
                    if (upper.Contains("I7-") || upper.Contains("7700"))
                        rec = "Prosesor tertinggi di kelasnya (4 Core / 8 Thread). Kinerja prosesor sudah sangat optimal untuk soket ini.";

                    return (
                        "Socket LGA1151 (Intel 6th/7th Gen)",
                        "Intel 100/200 Series (H110/B150/B250/Z270)",
                        "51W - 65W (Hemat Daya)",
                        "Intel(R) HD Graphics 530 / 630",
                        rec,
                        "Dapat Di-upgrade (Socket Modular)"
                    );
                }

                // Desktop Intel 4th / 5th Gen (Haswell / Broadwell)
                if (Regex.IsMatch(upper, @"I[3579]-[45]\d{3}") || upper.Contains("4130") || upper.Contains("4570") || upper.Contains("4770") || upper.Contains("G3220") || upper.Contains("G3258") || upper.Contains("G3260") || upper.Contains("G1820"))
                {
                    return (
                        "Socket LGA1150 (Intel 4th/5th Gen Haswell)",
                        "Intel 8/9 Series (H81/B85/H87/Z97)",
                        "54W - 84W",
                        "Intel(R) HD Graphics 4400 / 4600",
                        "Dapat di-upgrade ke Intel Core i5-4570 atau Core i7-4770 (4 Core / 8 Thread) untuk performa komputasi yang lebih responsif.",
                        "Dapat Di-upgrade (Socket Modular)"
                    );
                }

                // Desktop Intel 2nd / 3rd Gen (Sandy / Ivy Bridge)
                if (Regex.IsMatch(upper, @"I[3579]-[23]\d{3}") || upper.Contains("2100") || upper.Contains("3220") || upper.Contains("3470") || upper.Contains("3770") || upper.Contains("G2020") || upper.Contains("G620") || upper.Contains("G1610"))
                {
                    return (
                        "Socket LGA1155 (Intel 2nd/3rd Gen)",
                        "Intel 6/7 Series (H61/B75/H77/Z77)",
                        "55W - 77W",
                        "Intel(R) HD Graphics 2000 / 2500 / 4000",
                        "Dapat di-upgrade ke Intel Core i5-3470 atau Core i7-3770 (4 Core / 8 Thread).",
                        "Dapat Di-upgrade (Socket Modular)"
                    );
                }

                // Desktop Intel 1st Gen Core (Nehalem / Clarkdale)
                if (Regex.IsMatch(upper, @"I[357]-\d{3}\b") || upper.Contains("I5 750") || upper.Contains("I7 860") || upper.Contains("I7 870") || upper.Contains("I3 530") || upper.Contains("I5 650"))
                {
                    return (
                        "Socket LGA1156 (Intel 1st Gen Core)",
                        "Intel 5 Series (H55/P55/H57)",
                        "73W - 95W",
                        "Intel(R) HD Graphics",
                        "Dapat di-upgrade ke Intel Core i7-870 (4 Core / 8 Thread).",
                        "Dapat Di-upgrade (Socket Modular)"
                    );
                }

                // Legacy Core 2 Duo / Quad
                if (upper.Contains("CORE 2") || upper.Contains("CORE2") || upper.Contains("QUAD") || upper.Contains("DUO") || upper.Contains("E8400") || upper.Contains("Q6600") || upper.Contains("Q9400") || upper.Contains("Q9550"))
                {
                    return (
                        "Socket LGA775 (Intel Core 2 / Quad)",
                        "Intel G31/G41/P45 Series",
                        "65W - 95W",
                        "Intel(R) GMA Graphics",
                        "Platform lawas (LGA775). Disarankan mempertimbangkan peremajaan sistem ke generasi yang lebih modern untuk efisiensi daya dan kecepatan.",
                        "Platform Klasik (LGA775)"
                    );
                }

                return (
                    "Socket Modular (Intel Platform)",
                    "Intel Desktop Chipset",
                    "51W - 65W",
                    "Intel(R) HD/UHD Graphics",
                    "Mendukung upgrade prosesor yang kompatibel dengan soket motherboard Anda.",
                    "Dapat Di-upgrade"
                );
            }

            // ==========================================
            // 2. AMD PROCESSORS
            // ==========================================
            if (upper.Contains("AMD") || upper.Contains("RYZEN") || vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase))
            {
                // Laptop AMD APU
                if (isMobileCpu)
                {
                    return (
                        "BGA (Tersolder Terintegrasi)",
                        "AMD Integrated Mobile Chipset",
                        "15W - 35W (Hemat Daya)",
                        "AMD Radeon(TM) Graphics",
                        "Prosesor laptop ini terpasang permanen pada motherboard. Untuk akselerasi performa, kami menyarankan upgrade kapasitas RAM dan media penyimpanan SSD M.2 NVMe.",
                        "Tersolder (Fokus Upgrade RAM & SSD)"
                    );
                }

                // Desktop AMD Ryzen 7000 / 8000 / 9000 Series (Socket AM5)
                if (Regex.IsMatch(upper, @"RYZEN\s*[3579]\s*[789]\d{3}") || upper.Contains("7500F") || upper.Contains("7600") || upper.Contains("7700") || upper.Contains("7800") || upper.Contains("8600") || upper.Contains("8700") || upper.Contains("9600") || upper.Contains("9700") || upper.Contains("9800") || upper.Contains("9900") || upper.Contains("9950"))
                {
                    return (
                        "Socket AM5 (DDR5)",
                        "AMD 600/800 Series (A620/B650/X670/B850/X870)",
                        upper.Contains("X") ? "105W - 170W" : "65W (Standar AM5)",
                        "AMD Radeon(TM) Graphics",
                        "Platform Socket AM5 memiliki dukungan upgrade jangka panjang untuk prosesor generasi terbaru.",
                        "Dapat Di-upgrade (Socket AM5)"
                    );
                }

                // Desktop AMD Ryzen 1000 - 5000 Series (Socket AM4)
                if (Regex.IsMatch(upper, @"RYZEN\s*[3579]\s*[12345]\d{3}") || upper.Contains("1600") || upper.Contains("2600") || upper.Contains("3600") || upper.Contains("5500") || upper.Contains("5600") || upper.Contains("5700") || upper.Contains("5800") || upper.Contains("3700") || upper.Contains("3900") || upper.Contains("5900") || upper.Contains("5950") || upper.Contains("ATHLON 200GE") || upper.Contains("ATHLON 3000G") || upper.Contains("3200G") || upper.Contains("2200G") || upper.Contains("4600G") || upper.Contains("5600G"))
                {
                    string rec = "Dapat di-upgrade ke AMD Ryzen 7 5700X (8 Core / 16 Thread) atau Ryzen 7 5800X3D untuk performa kerja dan gaming yang jauh lebih bertenaga.";
                    if (upper.Contains("5700") || upper.Contains("5800") || upper.Contains("5900") || upper.Contains("5950"))
                        rec = "Prosesor kelas atas 8 - 16 Core yang sangat bertenaga. Kinerja sudah sangat optimal.";

                    return (
                        "Socket AM4 (DDR4)",
                        "AMD 300/400/500 Series (A320/B450/B550/X570)",
                        "65W - 105W",
                        upper.Contains("G") || upper.Contains("GE") ? "AMD Radeon(TM) Vega Graphics" : "Memerlukan Kartu Grafis Dedikasi (VGA)",
                        rec,
                        "Dapat Di-upgrade (Socket AM4)"
                    );
                }

                // Legacy AMD FX / FM2
                if (upper.Contains("FX-") || upper.Contains("FX ") || upper.Contains("A10-") || upper.Contains("A8-") || upper.Contains("A6-") || upper.Contains("A4-"))
                {
                    return (
                        upper.Contains("FX") ? "Socket AM3+ (DDR3)" : "Socket FM2 / FM2+",
                        "AMD 700/800/900 / A-Series Chipset",
                        "65W - 125W",
                        "AMD Radeon HD Graphics",
                        "Platform lawas (AM3+/FM2). Disarankan mempertimbangkan peremajaan sistem ke generasi AMD Ryzen untuk performa dan efisiensi modern.",
                        "Platform Klasik (DDR3)"
                    );
                }

                return (
                    "Socket AM4 / AM5",
                    "AMD Desktop Chipset",
                    "65W",
                    "AMD Radeon Graphics",
                    "Mendukung upgrade prosesor AMD sesuai soket motherboard Anda.",
                    "Dapat Di-upgrade"
                );
            }

            // ==========================================
            // 3. GENERIC / FALLBACK
            // ==========================================
            return (
                category == DeviceCategory.Laptop ? "BGA (Tersolder)" : "Socket Modular",
                "Integrated Chipset",
                "Standar Operasional",
                "Integrated Graphics",
                category == DeviceCategory.Laptop
                    ? "Prosesor terpasang permanen pada motherboard. Peningkatan kecepatan dilakukan melalui upgrade RAM dan media penyimpanan SSD."
                    : "Mendukung upgrade prosesor yang kompatibel dengan motherboard Anda.",
                category == DeviceCategory.Laptop ? "Tersolder (Fokus RAM & SSD)" : "Dapat Di-upgrade"
            );
        }

        /// <summary>
        /// Menganalisis konfigurasi RAM dan menghasilkan form factor, kapasitas maksimum, dan rekomendasi upgrade yang ramah konsumen.
        /// </summary>
        public static (string FormFactor, string MaxCapacity, string SlotsSummary, string ChannelMode, string Recommendation, string Feasibility)
            AnalyzeMemory(double totalGb, DeviceCategory category, string memoryType = "DDR4 SDRAM", string? modelName = null, int? explicitTotalSlots = null)
        {
            bool isLaptop = category == DeviceCategory.Laptop;
            bool isMiniPc = category == DeviceCategory.MiniPc;
            bool isAio = category == DeviceCategory.AllInOne;
            string formFactor = isLaptop ? "Laptop SO-DIMM (Ringkas)" : "Desktop UDIMM (Standar PC)";

            string upperModel = (modelName ?? "").ToUpperInvariant();

            // Tentukan jumlah total slot fisik motherboard
            int totalSlots;
            if (explicitTotalSlots.HasValue && explicitTotalSlots.Value > 0)
            {
                totalSlots = explicitTotalSlots.Value;
            }
            else if (isLaptop || isMiniPc || isAio || upperModel.Contains("H110") || upperModel.Contains("H310") || upperModel.Contains("H410") || upperModel.Contains("H510") || upperModel.Contains("H610") || upperModel.Contains("A320") || upperModel.Contains("A520") || upperModel.Contains("A620") || upperModel.Contains("ITX"))
            {
                totalSlots = 2;
            }
            else
            {
                // Desktop SFF (ThinkCentre M-series, OptiPlex SFF, ProDesk SFF) dan Desktop Tower (ATX/mATX B-series/Z-series) memiliki 4 slot DIMM
                totalSlots = 4;
            }

            // Tentukan estimasi keping terpasang berdasarkan total kapasitas terdeteksi
            int filledSlots;
            if (totalGb <= 4.5)
            {
                filledSlots = 1;
            }
            else if (totalGb <= 8.5)
            {
                filledSlots = 1;
            }
            else if (totalGb <= 16.5)
            {
                filledSlots = 2;
            }
            else if (totalGb <= 32.5)
            {
                filledSlots = 2;
            }
            else
            {
                filledSlots = totalSlots;
            }

            int emptySlots = Math.Max(0, totalSlots - filledSlots);

            // Format ringkasan slot
            string slotsSummary;
            if (emptySlots > 0)
            {
                slotsSummary = $"{filledSlots} dari {totalSlots} Slot Terisi ({emptySlots} Slot Kosong Tersedia)";
            }
            else
            {
                slotsSummary = $"Semua {totalSlots} Slot Terisi Penuh";
            }

            string maxCap = (totalSlots == 4)
                ? "Maksimal 64 GB - 128 GB (4 Slot DIMM)"
                : (isLaptop ? "Maksimal 32 GB - 64 GB (2 Slot SO-DIMM)" : "Maksimal 32 GB - 64 GB (2 Slot DIMM)");

            string channelMode = (filledSlots >= 2) ? "Dual-Channel Aktif (128-bit)" : "Single-Channel (Mendukung Dual-Channel)";

            string recommendation;
            string feasibility;

            if (filledSlots == 1 && totalSlots == 4)
            {
                recommendation = "Disarankan menambah 1 keping RAM 8 GB DDR4 pada slot kosong untuk mengaktifkan mode Dual-Channel (total 16 GB). Motherboard masih memiliki 2 slot cadangan untuk penambahan memori di masa mendatang hingga 64 GB.";
                feasibility = $"Sangat Fleksibel (Tersedia {emptySlots} Slot Kosong)";
            }
            else if (filledSlots == 1 && totalSlots == 2)
            {
                recommendation = isLaptop
                    ? "Disarankan menambah 1 keping RAM 8 GB SO-DIMM pada slot kosong untuk mengaktifkan mode Dual-Channel (total 16 GB), membuat laptop lebih gegas saat membuka banyak aplikasi sekaligus."
                    : "Disarankan menambah 1 keping RAM 8 GB DDR4 pada slot kosong untuk mengaktifkan mode Dual-Channel (total 16 GB), melipatgandakan kecepatan transfer data dan multitasking harian Anda.";
                feasibility = $"Mudah Di-upgrade (Tersedia {emptySlots} Slot Kosong)";
            }
            else if (filledSlots >= 2 && emptySlots > 0)
            {
                recommendation = $"Kapasitas {totalGb:F0} GB Dual-Channel sudah sangat ideal untuk kebutuhan harian dan multitasking kantor. Masih tersedia {emptySlots} slot kosong jika Anda ingin menambah RAM hingga kapasitas maksimal untuk beban komputasi berat.";
                feasibility = $"Optimal (Tersedia {emptySlots} Slot Kosong)";
            }
            else
            {
                recommendation = "Seluruh slot memori telah terpasang dalam konfigurasi optimal. Jika membutuhkan kapasitas lebih besar di kemudian hari, Anda dapat mengganti modul RAM yang ada dengan modul berkapasitas lebih tinggi.";
                feasibility = "Kapasitas Maksimal & Sangat Memadai";
            }

            return (formFactor, maxCap, slotsSummary, channelMode, recommendation, feasibility);
        }

        /// <summary>
        /// Menganalisis media penyimpanan (Storage) dan menghasilkan saran upgrade SSD/HDD yang ramah konsumen.
        /// </summary>
        public static (string FormFactor, string M2Status, string SataStatus, string Recommendation, string Feasibility, string EstimatedGain)
            AnalyzeStorage(string diskModel, bool isRotationalHdd, double totalGb, DeviceCategory category, bool? isNvmeExplicit = null, int driveCount = 1)
        {
            bool isLaptop = category == DeviceCategory.Laptop;
            string upper = (diskModel ?? "").ToUpperInvariant();
            bool isNvme = isNvmeExplicit ?? (
                upper.Contains("NVME") || upper.Contains("PCIE") || (diskModel ?? "").StartsWith("nvme", StringComparison.OrdinalIgnoreCase) ||
                upper.Contains("MTFDK") || upper.Contains("MZAL") ||
                upper.Contains("MZVL") || upper.Contains("MZ-V") || upper.Contains("MZV") || upper.Contains("SN750") ||
                upper.Contains("SN850") || upper.Contains("SN570") || upper.Contains("SN770") || upper.Contains("CT500P") ||
                upper.Contains("CT1000P") || upper.Contains("WD_BLACK") || upper.Contains("SSDPEK") || upper.Contains("SOLIDIGM") ||
                upper.Contains("MICRON") || upper.Contains("970 EVO") || upper.Contains("980 PRO") || upper.Contains("990 PRO")
            );

            if (driveCount >= 2)
            {
                if (isNvme)
                {
                    string formFactor = "Dual Slot SSD M.2 NVMe PCIe";
                    string m2Status = "2x Slot M.2 Terpasang Penuh (Dual NVMe)";
                    string sataStatus = isLaptop ? "Port Ekspansi Internal Aktif" : "Slot M.2 & Port SATA Terisi Optimal";
                    string recommendation = "Perangkat Anda telah menggunakan konfigurasi Dual NVMe SSD pada kedua slot fisik. Ruang sistem dan partisi data terdistribusi optimal dengan throughput transfer maksimal di kedua drive.";
                    string feasibility = "2 Slot Terpasang Penuh (Dual NVMe)";
                    string gain = "Kecepatan akses data, transfer antardrive, dan waktu loading aplikasi sudah berada pada performa puncak.";
                    return (formFactor, m2Status, sataStatus, recommendation, feasibility, gain);
                }
                else if (isRotationalHdd)
                {
                    string formFactor = "Multi-Drive (Hybrid SSD + HDD)";
                    string m2Status = "1x Slot M.2 NVMe / SATA Terpasang";
                    string sataStatus = "1x Bay SATA HDD Terpasang";
                    string recommendation = "Perangkat Anda menggunakan konfigurasi hybrid: SSD cepat untuk sistem operasi dan HDD berkapasitas besar untuk arsip data berkas.";
                    string feasibility = "Konfigurasi Hybrid Optimal";
                    string gain = "Booting cepat dari SSD dengan kapasitas penyimpanan lega dari HDD sekunder.";
                    return (formFactor, m2Status, sataStatus, recommendation, feasibility, gain);
                }
                else
                {
                    string formFactor = "Dual Solid State Drive (SSD)";
                    string m2Status = "Slot M.2 / SATA Aktif";
                    string sataStatus = "Port SATA / M.2 Sekunder Aktif";
                    string recommendation = "Perangkat Anda menggunakan konfigurasi multi-drive SSD. Kedua media penyimpanan berbasis flash disk yang responsif dan bebas risiko guncangan mekanik.";
                    string feasibility = "2 Drive SSD Aktif & Optimal";
                    string gain = "Responsivitas sistem dan partisi penyimpanan data kedua-duanya sangat cepat.";
                    return (formFactor, m2Status, sataStatus, recommendation, feasibility, gain);
                }
            }

            if (isRotationalHdd)
            {
                string formFactor = isLaptop ? "2.5\" Harddisk Drive (HDD)" : "3.5\" / 2.5\" Harddisk Drive (HDD)";
                string m2Status = isLaptop ? "Tersedia Slot SSD M.2 NVMe" : "1x Slot M.2 NVMe PCIe Siap Pasang";
                string sataStatus = isLaptop ? "Port SATA Terhubung ke HDD" : "Port SATA Cadangan Tersedia";
                string recommendation = "Sangat disarankan memasang SSD (M.2 NVMe atau SATA) sebesar 256 GB - 1 TB sebagai drive utama sistem operasi. Komputer akan menyala dan membuka aplikasi dalam hitungan detik, sementara harddisk lama tetap dapat digunakan untuk menyimpan file & data pribadi.";
                string feasibility = "Sangat Disarankan Upgrade ke SSD";
                string gain = "Kecepatan menyala (booting) dan membuka aplikasi meningkat drastis hingga 10x - 20x lebih cepat dibanding harddisk biasa.";

                return (formFactor, m2Status, sataStatus, recommendation, feasibility, gain);
            }
            else if (isNvme)
            {
                string formFactor = "SSD M.2 NVMe PCIe";
                string m2Status = isLaptop ? "1x Slot M.2 Terpasang (Slot Sekunder Siap Ekspansi)" : "Slot M.2 Terpasang SSD NVMe Cepat";
                string sataStatus = isLaptop ? "Port Ekspansi Internal Aktif" : "Tersedia Slot M.2 / SATA Tambahan";
                string recommendation = "Perangkat Anda sudah menggunakan SSD NVMe berkecepatan tinggi. Upgrade hanya diperlukan jika ruang penyimpanan mulai penuh dengan mengganti ke kapasitas lebih besar (1 TB / 2 TB) atau memasang drive sekunder.";
                string feasibility = "Performa Sudah Maksimal (NVMe)";
                string gain = "Kecepatan akses data dan responsivitas aplikasi sudah berada di tingkat tertinggi.";

                return (formFactor, m2Status, sataStatus, recommendation, feasibility, gain);
            }
            else
            {
                // SATA SSD
                string formFactor = "2.5\" Solid State Drive (SATA)";
                string m2Status = isLaptop ? "Dukungan M.2 NVMe (Opsional)" : "Tersedia Slot M.2 NVMe untuk Kecepatan Ekstra";
                string sataStatus = "Port SATA III 6 Gbps Aktif";
                string recommendation = "Perangkat Anda sudah menggunakan SSD SATA yang responsif. Anda dapat memperbesar kapasitas penyimpanan atau beralih ke NVMe SSD (jika perangkat mendukung) untuk kecepatan transfer file yang lebih tinggi.";
                string feasibility = "Bisa Ditambah Kapasitas / Upgrade NVMe";
                string gain = "Aplikasi sudah responsif; upgrade ke NVMe akan memberikan kecepatan ekstra saat transfer file berukuran besar.";

                return (formFactor, m2Status, sataStatus, recommendation, feasibility, gain);
            }
        }

        /// <summary>
        /// Menganalisis Motherboard & Form Factor untuk menghasilkan deskripsi ekspansi dan sistem daya yang ramah konsumen.
        /// </summary>
        public static (string ExpansionSlots, string PowerSupply, string UpgradeAdvice)
            AnalyzeMotherboard(string manufacturer, string productModel, DeviceCategory category)
        {
            switch (category)
            {
                case DeviceCategory.Laptop:
                    return (
                        "Slot SSD M.2 NVMe, Modul WiFi/Bluetooth, Port USB Type-C & HDMI",
                        "Adaptor Charger Eksternal + Baterai Li-Ion Terintegrasi",
                        "Pada perangkat laptop, komponen yang paling praktis dan aman untuk ditingkatkan performanya adalah penambahan RAM dan media penyimpanan SSD M.2 NVMe."
                    );

                case DeviceCategory.DesktopSff:
                    return (
                        "1x PCIe x16 (Low-Profile), PCIe x1, Slot SSD M.2 NVMe, Port SATA III",
                        "Internal Power Supply SFF (Hemat Daya & Stabil)",
                        "Perangkat komputer ringkas (SFF) ini siap menerima penambahan kartu grafis tipe Low-Profile (seperti NVIDIA GT 1030 / GTX 1650 LP) dan SSD NVMe tanpa perlu mengganti power supply."
                    );

                case DeviceCategory.MiniPc:
                    return (
                        "1-2x Slot SSD M.2 NVMe, Modul WiFi/BT, Port USB Type-C / Fast IO",
                        "Adaptor Daya Eksternal DC (Hemat Energi)",
                        "Mini PC mendukung upgrade media penyimpanan SSD M.2 NVMe dan modul RAM SO-DIMM dengan konsumsi daya yang sangat efisien."
                    );

                case DeviceCategory.AllInOne:
                    return (
                        "Slot SSD M.2 NVMe, Modul WiFi/BT, Port SATA, Port USB 3.2",
                        "Adaptor Daya Eksternal DC / Modul Daya Terintegrasi",
                        "Perangkat All-in-One mendukung upgrade modul RAM SO-DIMM dan pergantian penyimpanan SSD untuk menjaga performa kerja tetap responsif."
                    );

                case DeviceCategory.DesktopTower:
                default:
                    return (
                        "1x PCIe x16 (Full Height untuk Kartu Grafis), PCIe x1, Slot SSD M.2 NVMe, Port SATA III",
                        "Internal Power Supply Unit (ATX)",
                        "Komputer desktop memiliki kemudahan upgrade yang fleksibel: RAM, media penyimpanan SSD/HDD, kartu grafis dedicated (GPU), serta prosesor dapat ditingkatkan secara leluasa."
                    );
            }
        }
    }
}
