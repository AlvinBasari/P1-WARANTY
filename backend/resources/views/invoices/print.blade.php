<!DOCTYPE html>
<html lang="id">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Invoice Perbaikan & Klaim Garansi - {{ $invoice->invoice_number }}</title>
    <style>
        @page {
            size: A4;
            margin: 15mm;
        }
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif;
        }
        body {
            background-color: #f8fafc;
            color: #0f172a;
            font-size: 13px;
            line-height: 1.5;
            padding: 20px;
        }
        .invoice-container {
            max-width: 800px;
            margin: 0 auto;
            background: #ffffff;
            border-radius: 12px;
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.06);
            border: 1px solid #e2e8f0;
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #0f172a 0%, #1e293b 100%);
            color: #ffffff;
            padding: 30px;
            position: relative;
        }
        .header-top {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
        }
        .company-title {
            font-size: 22px;
            font-weight: 800;
            letter-spacing: -0.5px;
            color: #38bdf8;
        }
        .company-subtitle {
            font-size: 12px;
            color: #94a3b8;
            margin-top: 4px;
        }
        .invoice-badge-box {
            text-align: right;
        }
        .invoice-title {
            font-size: 20px;
            font-weight: 700;
            color: #ffffff;
            letter-spacing: 1px;
        }
        .invoice-num {
            font-size: 14px;
            font-weight: 600;
            color: #38bdf8;
            font-family: monospace;
            margin-top: 4px;
        }
        .info-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            padding: 24px 30px;
            background: #f8fafc;
            border-bottom: 1px solid #e2e8f0;
        }
        .info-block h4 {
            font-size: 11px;
            text-transform: uppercase;
            letter-spacing: 0.8px;
            color: #64748b;
            margin-bottom: 8px;
            font-weight: 700;
        }
        .info-block p {
            margin-bottom: 3px;
            color: #1e293b;
            font-size: 12.5px;
        }
        .info-block .bold {
            font-weight: 600;
        }
        .content {
            padding: 30px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 24px;
        }
        th {
            background: #f1f5f9;
            color: #475569;
            text-transform: uppercase;
            font-size: 11px;
            font-weight: 700;
            letter-spacing: 0.5px;
            padding: 12px 14px;
            text-align: left;
            border-bottom: 2px solid #cbd5e1;
        }
        th.text-right, td.text-right {
            text-align: right;
        }
        th.text-center, td.text-center {
            text-align: center;
        }
        td {
            padding: 14px;
            border-bottom: 1px solid #e2e8f0;
            vertical-align: middle;
        }
        tr:hover {
            background-color: #f8fafc;
        }
        .badge {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 6px;
            font-size: 10px;
            font-weight: 700;
            text-transform: uppercase;
        }
        .badge-warranty {
            background-color: #dcfce7;
            color: #15803d;
            border: 1px solid #86efac;
        }
        .badge-client {
            background-color: #fee2e2;
            color: #b91c1c;
            border: 1px solid #fca5a5;
        }
        .summary-card {
            display: flex;
            justify-content: flex-end;
            margin-bottom: 24px;
        }
        .summary-table {
            width: 340px;
            border-collapse: collapse;
        }
        .summary-table td {
            padding: 8px 12px;
            border: none;
        }
        .summary-table .border-top {
            border-top: 1px solid #cbd5e1;
        }
        .summary-table .grand-total {
            font-size: 16px;
            font-weight: 800;
            background: #f0fdf4;
            color: #166534;
            border-radius: 8px;
        }
        .stamp-box {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-top: 20px;
            border-top: 2px dashed #e2e8f0;
        }
        .terms {
            max-width: 460px;
            font-size: 11px;
            color: #64748b;
        }
        .terms h5 {
            font-size: 11.5px;
            color: #334155;
            margin-bottom: 4px;
            font-weight: 700;
        }
        .official-seal {
            border: 2px solid #0284c7;
            border-radius: 8px;
            padding: 10px 16px;
            text-align: center;
            background: #f0f9ff;
            color: #0369a1;
        }
        .official-seal .title {
            font-size: 10px;
            font-weight: 800;
            letter-spacing: 1px;
            text-transform: uppercase;
        }
        .official-seal .status {
            font-size: 13px;
            font-weight: 800;
            margin: 4px 0;
            color: #0284c7;
        }
        .actions-bar {
            background: #f1f5f9;
            padding: 16px 30px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-top: 1px solid #e2e8f0;
        }
        .btn-print {
            background: #0284c7;
            color: #ffffff;
            border: none;
            padding: 10px 20px;
            border-radius: 6px;
            font-weight: 700;
            font-size: 13px;
            cursor: pointer;
            box-shadow: 0 2px 8px rgba(2, 132, 199, 0.3);
        }
        .btn-print:hover {
            background: #0369a1;
        }
        @media print {
            body {
                background: #ffffff;
                padding: 0;
            }
            .invoice-container {
                box-shadow: none;
                border: none;
                max-width: 100%;
            }
            .actions-bar {
                display: none;
            }
        }
    </style>
</head>
<body>

<div class="invoice-container">
    <!-- Header -->
    <div class="header">
        <div class="header-top">
            <div>
                <div class="company-title">PT JAYA TEKNOLOGI SOLUSINDO</div>
                <div class="company-subtitle">Sentra Layanan &amp; Klaim Garansi Resmi Hardware Terpadu</div>
                <div style="font-size: 11px; color: #94a3b8; margin-top: 6px;">
                    Cyber 2 Tower Lt. 18, Jl. H.R. Rasuna Said, Jakarta Selatan • Hotline: (021) 5088-7799
                </div>
            </div>
            <div class="invoice-badge-box">
                <div class="invoice-title">FAKTUR SERVIS RESMI</div>
                <div class="invoice-num">{{ $invoice->invoice_number }}</div>
                <div style="font-size: 11px; color: #cbd5e1; margin-top: 4px;">
                    Tanggal: {{ \Carbon\Carbon::parse($invoice->issue_date)->format('d M Y, H:i') }} WIB
                </div>
            </div>
        </div>
    </div>

    <!-- Info Grid -->
    <div class="info-grid">
        <div class="info-block">
            <h4>Data Pelanggan &amp; Lokasi</h4>
            <p class="bold">{{ $invoice->user->name ?? 'Pelanggan JTS' }}</p>
            <p>Email: {{ $invoice->user->email ?? '-' }}</p>
            <p>Telp / WA: {{ $invoice->user->phone ?? '-' }}</p>
            <p>Alamat: {{ $invoice->user->address ?? 'Alamat Terkalibrasi GPS' }}</p>
        </div>
        <div class="info-block">
            <h4>Informasi Unit &amp; Garansi</h4>
            <p><span class="bold">Model Unit:</span> {{ $invoice->device->model ?? '-' }}</p>
            <p><span class="bold">Serial Number:</span> <code>{{ $invoice->device->serial_number ?? '-' }}</code></p>
            <p><span class="bold">ID Hardware / BIOS:</span> <code>{{ $invoice->device->hardware_id ?? '-' }}</code></p>
            <p><span class="bold">Tiket Perbaikan:</span> #{{ $invoice->repair_request_id }} (Tipe: {{ strtoupper($invoice->repairRequest->type ?? 'ON_SITE') }})</p>
        </div>
    </div>

    <!-- Items Table -->
    <div class="content">
        <table>
            <thead>
                <tr>
                    <th style="width: 5%;">No</th>
                    <th style="width: 40%;">Rincian Tindakan / Suku Cadang</th>
                    <th class="text-center" style="width: 15%;">Garansi</th>
                    <th class="text-center" style="width: 8%;">Qty</th>
                    <th class="text-right" style="width: 16%;">Harga Satuan</th>
                    <th class="text-right" style="width: 16%;">Subtotal</th>
                </tr>
            </thead>
            <tbody>
                @foreach ($invoice->items as $idx => $item)
                <tr>
                    <td class="text-center">{{ $idx + 1 }}</td>
                    <td>
                        <div style="font-weight: 600; color: #0f172a;">{{ $item->item_name }}</div>
                        @if ($item->item_code)
                            <div style="font-size: 11px; color: #64748b;">Part No: <code>{{ $item->item_code }}</code></div>
                        @endif
                        @if ($item->notes)
                            <div style="font-size: 11px; color: #475569; font-style: italic;">{{ $item->notes }}</div>
                        @endif
                    </td>
                    <td class="text-center">
                        @if ($item->is_covered_by_warranty)
                            <span class="badge badge-warranty">🛡️ Tercover Garansi</span>
                        @else
                            <span class="badge badge-client">⚠️ Biaya Klien</span>
                        @endif
                    </td>
                    <td class="text-center">{{ $item->quantity }}</td>
                    <td class="text-right">Rp {{ number_format($item->unit_price, 0, ',', '.') }}</td>
                    <td class="text-right">Rp {{ number_format($item->subtotal, 0, ',', '.') }}</td>
                </tr>
                @endforeach
            </tbody>
        </table>

        <!-- Summary Calculation -->
        <div class="summary-card">
            <table class="summary-table">
                <tr>
                    <td style="color: #64748b;">Subtotal Biaya Normal:</td>
                    <td class="text-right bold">Rp {{ number_format($invoice->subtotal_amount, 0, ',', '.') }}</td>
                </tr>
                <tr>
                    <td style="color: #16a34a; font-weight: 600;">Jaminan Garansi Resmi PT JTS:</td>
                    <td class="text-right bold" style="color: #16a34a;">- Rp {{ number_format($invoice->warranty_discount_amount, 0, ',', '.') }}</td>
                </tr>
                @if ($invoice->tax_amount > 0)
                <tr>
                    <td style="color: #64748b;">PPN (11%):</td>
                    <td class="text-right bold">Rp {{ number_format($invoice->tax_amount, 0, ',', '.') }}</td>
                </tr>
                @endif
                <tr class="border-top grand-total">
                    <td style="padding: 12px;">Total Tagihan Klien:</td>
                    <td class="text-right" style="padding: 12px;">Rp {{ number_format($invoice->total_payable_amount, 0, ',', '.') }}</td>
                </tr>
            </table>
        </div>

        <!-- Stamp & Terms -->
        <div class="stamp-box">
            <div class="terms">
                <h5>Syarat &amp; Ketentuan Garansi Resmi:</h5>
                <p>1. Seluruh suku cadang resmi yang diganti terdaftar dan dilindungi jaminan garansi 1 tahun.</p>
                <p>2. Pemotongan jaminan garansi 100% berlaku otomatis sesuai cakupan perlindungan aktif unit.</p>
                <p>3. Dokumen ini sah dan diterbitkan secara digital oleh Pusat Servis Resmi PT Jaya Teknologi Solusindo.</p>
            </div>
            <div class="official-seal">
                <div class="title">VERIFIKASI SISTEM GARANSI</div>
                <div class="status">
                    @if ($invoice->total_payable_amount <= 0)
                        ✓ LUNAS (DIJAMIN 100%)
                    @else
                        TAGIHAN KLIEN
                    @endif
                </div>
                <div style="font-size: 10px; color: #64748b;">Teknisi: {{ $invoice->technician->name ?? 'Tim Servis Resmi' }}</div>
            </div>
        </div>
    </div>

    <!-- Actions Bar -->
    <div class="actions-bar">
        <div style="font-size: 12px; color: #64748b;">
            💡 Simpan atau cetak faktur ini sebagai arsip riwayat servis perangkat Anda.
        </div>
        <button class="btn-print" onclick="window.print()">🖨️ Cetak / Simpan sebagai PDF</button>
    </div>
</div>

</body>
</html>
