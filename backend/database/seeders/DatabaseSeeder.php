<?php

namespace Database\Seeders;

use App\Models\User;
use App\Models\Device;
use App\Models\Warranty;
use App\Models\RepairRequest;
use App\Models\RemoteSession;
use App\Models\RepairTracking;
use App\Models\RepairTrackingHistory;
use App\Models\Invoice;
use App\Models\InvoiceItem;
use Illuminate\Database\Seeder;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;

class DatabaseSeeder extends Seeder
{
    /**
     * Seed the application's database.
     */
    public function run(): void
    {
        // 1. Create Internal Staff & Demo Users
        $admin = User::create([
            'name' => 'Admin Garansi',
            'email' => 'admin@warranty.com',
            'phone' => '081122334455',
            'address' => 'Head Office Service Center Lt. 3, Jakarta Pusat',
            'role' => 'admin',
            'password' => Hash::make('password'),
        ]);

        $technician = User::create([
            'name' => 'Budi Santoso (Teknisi)',
            'email' => 'technician@warranty.com',
            'phone' => '081234567890',
            'address' => 'Workshop Service Center, Jakarta Barat',
            'role' => 'technician',
            'password' => Hash::make('password'),
        ]);

        $customer = User::create([
            'name' => 'Ahmad Pratama',
            'email' => 'customer@example.com',
            'phone' => '085712345678',
            'address' => 'Jl. Tebet Raya No. 12, Jakarta Selatan',
            'role' => 'customer',
            'password' => Hash::make('password'),
        ]);

        // 2. Create Master Devices & Warranties
        $device1 = Device::create([
            'hardware_id' => 'BIOS-UUID-DEMO-001',
            'model' => 'ASUS ZenBook Pro 14 OLED (UX6404)',
            'serial_number' => 'SN-ASUS-982341',
            'user_id' => $customer->id,
            'qr_token' => 'qr-demo-asus-001',
            'location_lat' => -6.229728,
            'location_lng' => 106.855845,
            'location_label' => 'Rumah (Tebet, Jakarta Selatan)',
        ]);

        Warranty::create([
            'device_id' => $device1->id,
            'purchase_date' => '2025-01-15',
            'warranty_start' => '2025-01-15',
            'warranty_end' => '2027-01-15',
            'status' => 'active',
        ]);

        $device2 = Device::create([
            'hardware_id' => 'MACHINE-f561d272e3324caca6df3ce4046f7c25',
            'model' => 'Lenovo ThinkCentre M700 (10MAS0FB00)',
            'serial_number' => 'SN-LEN-112233',
            'user_id' => $customer->id,
            'qr_token' => 'qr-demo-lenovo-002',
            'location_lat' => -6.229728,
            'location_lng' => 106.855845,
            'location_label' => 'Kantor JTS / Rumah',
        ]);

        Warranty::create([
            'device_id' => $device2->id,
            'purchase_date' => '2025-05-10',
            'warranty_start' => '2025-05-10',
            'warranty_end' => '2027-05-10',
            'status' => 'active',
        ]);

        $device3 = Device::create([
            'hardware_id' => 'BIOS-UUID-DEMO-003',
            'model' => 'Acer Predator Helios Neo 16',
            'serial_number' => 'SN-ACER-778899',
            'user_id' => $customer->id,
            'qr_token' => 'qr-demo-acer-003',
            'location_lat' => -6.229728,
            'location_lng' => 106.855845,
            'location_label' => 'Rumah',
        ]);

        Warranty::create([
            'device_id' => $device3->id,
            'purchase_date' => '2024-02-01',
            'warranty_start' => '2024-02-01',
            'warranty_end' => '2026-02-01',
            'status' => 'active',
        ]);

        // 3. Demo Request 1: Remote Support (Non-Fisik)
        $remoteRequest = RepairRequest::create([
            'device_id' => $device1->id,
            'user_id' => $customer->id,
            'damage_category' => 'non_physical',
            'type' => 'remote',
            'description' => 'Layar flickering saat membuka aplikasi berat dan driver VGA sering crash (Error code 43).',
            'attachments' => ['screenshot_crash.png'],
            'location_lat' => $device1->location_lat,
            'location_lng' => $device1->location_lng,
            'location_confirmed_by_admin' => true,
            'needs_office_repair' => false,
            'status' => 'in_progress',
        ]);

        RemoteSession::create([
            'repair_request_id' => $remoteRequest->id,
            'technician_id' => $technician->id,
            'rustdesk_session_id' => '948123567',
            'started_at' => now()->subMinutes(15),
            'connection_status' => 'connected',
            'notes' => 'Sedang melakukan reinstall driver GPU NVIDIA via DDU.',
        ]);

        // 4. Demo Request 2: On-site Servis Fisik (Layar pecah & butuh konfirmasi lokasi)
        $onsiteRequest = RepairRequest::create([
            'device_id' => $device3->id,
            'user_id' => $customer->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => 'Engsel laptop patah dan casing belakang retak akibat terbentur.',
            'attachments' => ['foto_engsel_patah.jpg', 'foto_casing.jpg'],
            'location_lat' => $device3->location_lat,
            'location_lng' => $device3->location_lng,
            'location_confirmed_by_admin' => false, // Perlu di-approve admin
            'needs_office_repair' => false,
            'status' => 'pending',
        ]);

        // 5. Demo Request 3: Unit dibawa ke kantor dengan tracking history lengkap
        $trackingRequest = RepairRequest::create([
            'device_id' => $device1->id,
            'user_id' => $customer->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => 'Motherboard short circuit, mati total setelah tersiram air.',
            'attachments' => ['foto_kondisi.jpg'],
            'location_lat' => $device1->location_lat,
            'location_lng' => $device1->location_lng,
            'location_confirmed_by_admin' => true,
            'needs_office_repair' => true,
            'status' => 'in_progress',
        ]);

        $tracking = RepairTracking::create([
            'repair_request_id' => $trackingRequest->id,
            'current_status' => 'sedang_diperbaiki',
        ]);

        RepairTrackingHistory::create([
            'repair_tracking_id' => $tracking->id,
            'status' => 'dijemput',
            'changed_by' => $technician->id,
            'notes' => 'Unit laptop berhasil dijemput oleh kurir teknisi lapangan dari alamat customer.',
            'created_at' => now()->subDays(2),
        ]);

        RepairTrackingHistory::create([
            'repair_tracking_id' => $tracking->id,
            'status' => 'di_service_center',
            'changed_by' => $admin->id,
            'notes' => 'Unit tiba di Service Center Pusat dan telah diregistrasi ke ruang diagnosa hardware.',
            'created_at' => now()->subDays(1),
        ]);

        RepairTrackingHistory::create([
            'repair_tracking_id' => $tracking->id,
            'status' => 'sedang_diperbaiki',
            'changed_by' => $technician->id,
            'notes' => 'Proses ultrasonic cleaning motherboard dan penggantian IC power regulator.',
            'created_at' => now()->subHours(4),
        ]);

        // Invoice for Device 1 (ASUS ZenBook)
        $asusInvoice = Invoice::create([
            'invoice_number' => 'INV-202609-0001',
            'repair_request_id' => $trackingRequest->id,
            'device_id' => $device1->id,
            'user_id' => $customer->id,
            'technician_id' => $technician->id,
            'issue_date' => now()->subHours(3),
            'due_date' => now()->addDays(7),
            'subtotal_amount' => 2100000,
            'warranty_discount_amount' => 2100000,
            'tax_amount' => 0,
            'total_payable_amount' => 0,
            'payment_status' => 'paid_by_warranty',
            'payment_method' => 'warranty_claim',
            'notes' => 'Faktur resmi klaim garansi PT JTS. Seluruh penggantian IC Power dan jasa perbaikan motherboard ditanggung garansi resmi 100%.',
            'terms_and_conditions' => "1. Seluruh suku cadang pengganti resmi dilindungi garansi 1 tahun.\n2. Pemotongan jaminan garansi 100% otomatis diaplikasikan.",
        ]);

        InvoiceItem::create([
            'invoice_id' => $asusInvoice->id,
            'item_name' => 'Motherboard Power IC Regulator & Filter Capacitor OEM',
            'item_code' => 'ASUS-IC-PWR-V2',
            'category' => 'sparepart',
            'quantity' => 1,
            'unit_price' => 1850000,
            'subtotal' => 1850000,
            'is_covered_by_warranty' => true,
            'warranty_coverage_amount' => 1850000,
            'customer_payable_amount' => 0,
            'notes' => 'Garansi 1 Tahun Resmi PT JTS',
        ]);

        InvoiceItem::create([
            'invoice_id' => $asusInvoice->id,
            'item_name' => 'Jasa Ultrasonic Cleaning & Soldering IC Motherboard Lab',
            'item_code' => 'JTS-SVC-MB-01',
            'category' => 'service_fee',
            'quantity' => 1,
            'unit_price' => 250000,
            'subtotal' => 250000,
            'is_covered_by_warranty' => true,
            'warranty_coverage_amount' => 250000,
            'customer_payable_amount' => 0,
            'notes' => 'Uji kestabilan lolos QC',
        ]);

        // 6. Demo Request 4: Lenovo ThinkCentre M700 (SN-LEN-112233) - Tracking Workshop & Invoice Garansi
        $lenovoRequest = RepairRequest::create([
            'device_id' => $device2->id,
            'user_id' => $customer->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => 'Unit Lenovo ThinkCentre mati mendadak saat komputasi tinggi. Diagnosa teknisi: drop tegangan pada Power Supply Unit (PSU) dan thermal throttling.',
            'attachments' => ['lenovo_m700_diagnosa.jpg', 'psu_error.png'],
            'location_lat' => $device2->location_lat,
            'location_lng' => $device2->location_lng,
            'location_confirmed_by_admin' => true,
            'needs_office_repair' => true,
            'preferred_schedule' => 'Jadwal Penjemputan Cepat (Sesi Pagi 09:00 - 11:00 WIB)',
            'status' => 'in_progress',
        ]);

        $lenovoTracking = RepairTracking::create([
            'repair_request_id' => $lenovoRequest->id,
            'current_status' => 'sedang_diperbaiki',
        ]);

        RepairTrackingHistory::create([
            'repair_tracking_id' => $lenovoTracking->id,
            'status' => 'dijemput',
            'changed_by' => $technician->id,
            'notes' => 'Unit Lenovo ThinkCentre M700 berhasil dijemput oleh teknisi Budi Santoso di alamat pelanggan. Kondisi fisik diterima utuh dengan kabel power.',
            'created_at' => now()->subDays(2),
        ]);

        RepairTrackingHistory::create([
            'repair_tracking_id' => $lenovoTracking->id,
            'status' => 'di_service_center',
            'changed_by' => $admin->id,
            'notes' => 'Unit tiba di Workshop Service Center Pusat PT JTS. Lolos registrasi penerimaan intake barang dan dialokasikan ke Meja Diagnosa Lab Hardware #3.',
            'created_at' => now()->subDays(1),
        ]);

        RepairTrackingHistory::create([
            'repair_tracking_id' => $lenovoTracking->id,
            'status' => 'sedang_diperbaiki',
            'changed_by' => $technician->id,
            'notes' => 'Hasil diagnosa: Drop tegangan 12V rail pada PSU OEM. Sedang dilakukan penggantian modul Power Supply Unit (PSU) 250W OEM Lenovo dan repasting thermal paste Arctic MX-4. Uji kestabilan voltase sedang berlangsung.',
            'created_at' => now()->subHours(3),
        ]);

        $lenovoInvoice = Invoice::create([
            'invoice_number' => 'INV-202609-2002',
            'repair_request_id' => $lenovoRequest->id,
            'device_id' => $device2->id,
            'user_id' => $customer->id,
            'technician_id' => $technician->id,
            'issue_date' => now()->subHours(2),
            'due_date' => now()->addDays(7),
            'subtotal_amount' => 1250000,
            'warranty_discount_amount' => 1250000,
            'tax_amount' => 0,
            'total_payable_amount' => 0,
            'payment_status' => 'paid_by_warranty',
            'payment_method' => 'warranty_claim',
            'notes' => 'Faktur klaim garansi resmi PT JTS untuk unit Lenovo ThinkCentre M700. Seluruh biaya suku cadang OEM & jasa teknisi dicakup garansi 100%.',
            'terms_and_conditions' => "1. Komponen baru (PSU Lenovo 250W OEM) dilindungi garansi resmi selama 1 tahun.\n2. Klaim garansi mencakup 100% suku cadang dan jasa lab perbaikan tanpa biaya tambahan bagi pelanggan.\n3. Harap simpan faktur elektronik ini sebagai bukti servis resmi PT JTS.",
        ]);

        InvoiceItem::create([
            'invoice_id' => $lenovoInvoice->id,
            'item_name' => 'Power Supply Unit (PSU) Lenovo ThinkCentre 250W 80+ Bronze OEM',
            'item_code' => 'PSU-LEN-M700-250W',
            'category' => 'sparepart',
            'quantity' => 1,
            'unit_price' => 850000,
            'subtotal' => 850000,
            'is_covered_by_warranty' => true,
            'warranty_coverage_amount' => 850000,
            'customer_payable_amount' => 0,
            'notes' => 'Garansi Resmi Suku Cadang 1 Tahun PT JTS',
        ]);

        InvoiceItem::create([
            'invoice_id' => $lenovoInvoice->id,
            'item_name' => 'Thermal Compound Arctic MX-4 & Deep Cleaning Heatsink Fan',
            'item_code' => 'JTS-SVC-THP-01',
            'category' => 'sparepart',
            'quantity' => 1,
            'unit_price' => 150000,
            'subtotal' => 150000,
            'is_covered_by_warranty' => true,
            'warranty_coverage_amount' => 150000,
            'customer_payable_amount' => 0,
            'notes' => 'Perawatan Termal Komputer',
        ]);

        InvoiceItem::create([
            'invoice_id' => $lenovoInvoice->id,
            'item_name' => 'Jasa Servis, Penggantian Modul & Stress Test Voltase Lab',
            'item_code' => 'JTS-LAB-TEST-01',
            'category' => 'service_fee',
            'quantity' => 1,
            'unit_price' => 250000,
            'subtotal' => 250000,
            'is_covered_by_warranty' => true,
            'warranty_coverage_amount' => 250000,
            'customer_payable_amount' => 0,
            'notes' => 'Uji kestabilan benchmark AIDA64 lolos 60 menit',
        ]);
    }
}
