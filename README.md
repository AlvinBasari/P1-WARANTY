# Sistem Manajemen Garansi Produk dengan Fitur Remote Assistance

Sistem komprehensif untuk mengotomatisasi proses klaim garansi, diagnosis jarak jauh melalui **RustDesk**, manajemen servis on-site dengan kalibrasi lokasi otomatis, linimasa perbaikan di service center, dan jalur alternatif scan **QR Code** untuk perangkat mati total.

---

## 🏛️ Arsitektur & Komponen Sistem

```
┌────────────────────────────────────────────────────────┐
│             Desktop App (Customer & Teknisi)           │
│             .NET 8 (Avalonia UI C# / Fluent Theme)     │
│  - Auto-ekstraksi identitas BIOS/Hardware ID saat reg │
│  - Info Garansi & Kalibrasi Titik Lokasi Peta          │
│  - Request Bantuan Remote / Servis On-Site             │
│  - Stepper Linimasa Perbaikan Unit                     │
└───────────────────────────┬────────────────────────────┘
                            │ REST API (Sanctum)
                            ▼
┌────────────────────────────────────────────────────────┐
│                   Backend API — Laravel                │
│  - Auth Sanctum, Device & BIOS Management              │
│  - Repair Request & On-Site Location Confirmation      │
│  - Repair Tracking (5-Stage Stepper & Audit Log)       │
│  - Remote Session Logging & QR Token Lookup            │
└─────────────┬────────────────────────────┬─────────────┘
              │                            │
              ▼                            ▼
┌───────────────────────────┐   ┌────────────────────────┐
│ Admin Panel (React + Vite)│   │ Portal QR Publik (Web) │
│ - Real-time Dashboard     │   │ - Akses via scan QR    │
│ - Verifikasi Peta Lokasi  │   │ - Form klaim unit mati │
│ - 1-Klik Launch RustDesk  │   │ - Upload bukti foto    │
│ - Stepper Update Tracking │   └────────────────────────┘
└───────────────────────────┘
```

---

## 🚀 Cara Menjalankan Layanan

Setiap modul dapat dijalankan secara independen melalui script yang telah disediakan:

### 1. Jalankan Backend Laravel API (Port 8000)
```bash
./start-backend.sh
# Endpoint API: http://localhost:8000/api
```

### 2. Jalankan Admin Panel Web (Port 5173)
```bash
./start-admin.sh
# Akses browser: http://localhost:5173
```

### 3. Jalankan Portal Publik QR Code (Port 5174)
```bash
./start-public.sh
# Akses browser: http://localhost:5174/claim/qr-demo-lenovo-002
```

### 4. Jalankan Aplikasi Desktop Customer (Avalonia UI)
```bash
./run-customer-app.sh
```

### 5. Jalankan Launcher Teknisi (Protocol Handler)
```bash
./run-technician-app.sh yourapp://connect?id=948123567
```

---

## 🔑 Akun Demo Bawaan (Seeder)

| Peran | Email | Password | Keterangan |
|---|---|---|---|
| **Admin Utama** | `admin@warranty.com` | `password` | Kelola master data garansi, tiket masuk, konfirmasi peta lokasi |
| **Teknisi Lapangan** | `technician@warranty.com` | `password` | Remote diagnosis, tandai hasil kunjungan, update status tracking |
| **Customer Demo** | `customer@example.com` | `password` | Pemilik unit ASUS ZenBook & Acer Predator |

---

## 📋 Alur Data Utama Sesuai PRD

### 1. Registrasi Pertama Kali & Auto-Read BIOS ID (FR-01, FR-12)
- Saat registrasi di Customer Desktop App, `DeviceIdentifierService` otomatis membaca Motherboard/BIOS UUID.
- Akun customer langsung terikat dengan unit perangkat dan masa garansi aktif tanpa input serial number manual.

### 2. Klaim Kerusakan Non-Fisik / Software (FR-03, FR-04, FR-06)
- Customer memilih kategori **Non-Fisik** -> App otomatis menghasilkan **RustDesk Session ID**.
- Tiket masuk ke Admin Panel -> Teknisi klik tombol **Connect** (1-klik via protokol `yourapp://connect`).

### 3. Klaim Kerusakan Fisik / On-Site (FR-13, FR-14, FR-15)
- Customer memilih kategori **Fisik** -> Sistem melewati tahap remote dan otomatis melampirkan titik lokasi tersimpan.
- Admin meninjau koordinat pada peta interaktif dan menekan tombol **"Konfirmasi Lokasi"** untuk penugasan teknisi lapangan.

### 4. Servis di Kantor / Service Center (FR-18, FR-19, FR-20, FR-21)
- Teknisi lapangan menandai kunjungan sebagai **"Perlu dibawa ke kantor"**.
- Sistem membuat entri tracking dengan 5 tahapan:
  `Dijemput → Di Service Center → Sedang Diperbaiki → Selesai → Dikembalikan`
- Customer dan Admin memantau linimasa progres dan catatan teknisi secara real-time.

### 5. Jalur Alternatif Scan QR Code (FR-07, FR-08)
- Perangkat mati total / layar rusak di-scan via QR Code stiker fisik.
- Customer mengisi deskripsi kerusakan, kontak, dan titik GPS tanpa perlu login.
- Tiket langsung masuk ke antrean admin untuk penjemputan fisik.

---

## 🧪 Pengujian Otomatis (Automated Tests)

- **Backend API Tests (PHPUnit)**:
  ```bash
  cd backend && php artisan test
  ```
  *(10 Tests, 28 Assertions — 100% Passed)*

- **Desktop App Tests (xUnit)**:
  ```bash
  cd desktop-app && dotnet test
  ```
  *(4 Tests — 100% Passed)*
