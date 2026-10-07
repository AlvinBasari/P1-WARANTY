<!DOCTYPE html>
<html lang="id">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Faktur Servis Resmi & Jaminan Garansi - {{ $invoice->invoice_number }}</title>
    <style>
        @page {
            size: A4;
            margin: 12mm 15mm;
        }
        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
        }
        body {
            background-color: #F8FAFC;
            color: #0F172A;
            font-size: 12px;
            line-height: 1.5;
            padding: 24px;
        }
        .invoice-container {
            max-width: 820px;
            margin: 0 auto;
            background: #FFFFFF;
            border: 1px solid #CBD5E1;
            border-radius: 4px;
            box-shadow: 0 4px 16px rgba(15, 23, 42, 0.06);
            overflow: hidden;
        }

        /* Formal Kop Surat Perusahaan */
        .kop-header {
            padding: 24px 30px 20px;
            border-bottom: 2px solid #0F172A;
            background: #FFFFFF;
        }
        .kop-top {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            gap: 20px;
        }
        .company-identity {
            flex: 1;
        }
        .company-brand-row {
            display: flex;
            align-items: center;
            gap: 10px;
            margin-bottom: 4px;
        }
        .company-logo-badge {
            background: #0F172A;
            color: #FFFFFF;
            font-size: 13px;
            font-weight: 800;
            letter-spacing: 1px;
            padding: 3px 8px;
            border-radius: 3px;
            font-family: 'Consolas', 'Courier New', monospace;
        }
        .company-name {
            font-size: 17px;
            font-weight: 800;
            letter-spacing: 0.2px;
            color: #0F172A;
        }
        .company-dept {
            font-size: 11px;
            font-weight: 600;
            color: #475569;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 4px;
        }
        .company-meta {
            font-size: 10.5px;
            color: #64748B;
            line-height: 1.45;
        }

        .doc-identity {
            text-align: right;
            min-width: 250px;
        }
        .doc-title {
            font-size: 15px;
            font-weight: 800;
            letter-spacing: 0.8px;
            color: #0F172A;
            text-transform: uppercase;
        }
        .doc-subtitle {
            font-size: 10.5px;
            color: #64748B;
            font-weight: 600;
            margin-top: 2px;
            letter-spacing: 0.3px;
        }
        .doc-number {
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 13px;
            font-weight: 700;
            color: #0F172A;
            background: #F1F5F9;
            padding: 4px 8px;
            border-radius: 3px;
            display: inline-block;
            margin-top: 6px;
            border: 1px solid #E2E8F0;
        }
        .doc-date {
            font-size: 10.5px;
            color: #64748B;
            margin-top: 4px;
        }

        /* Info Grid (Pelanggan & Perangkat) */
        .info-grid {
            display: grid;
            grid-template-columns: 1fr 1fr;
            border-bottom: 1px solid #E2E8F0;
            background: #FAFAFA;
        }
        .info-block {
            padding: 16px 30px;
        }
        .info-block:first-child {
            border-right: 1px solid #E2E8F0;
        }
        .info-title {
            font-size: 10px;
            font-weight: 800;
            text-transform: uppercase;
            letter-spacing: 0.8px;
            color: #475569;
            margin-bottom: 8px;
            padding-bottom: 4px;
            border-bottom: 1px solid #E2E8F0;
        }
        .info-row {
            display: flex;
            margin-bottom: 4px;
            font-size: 11.5px;
        }
        .info-label {
            width: 110px;
            color: #64748B;
            flex-shrink: 0;
        }
        .info-value {
            color: #0F172A;
            font-weight: 500;
            flex: 1;
        }
        .info-value.bold {
            font-weight: 700;
        }
        .info-value code {
            font-family: 'Consolas', 'Courier New', monospace;
            background: #E2E8F0;
            padding: 1px 4px;
            border-radius: 2px;
            font-size: 11px;
        }

        /* Items Table */
        .content {
            padding: 24px 30px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }
        th {
            background: #F1F5F9;
            color: #334155;
            font-size: 10.5px;
            font-weight: 700;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            padding: 10px 12px;
            text-align: left;
            border-top: 1px solid #CBD5E1;
            border-bottom: 2px solid #0F172A;
        }
        td {
            padding: 11px 12px;
            border-bottom: 1px solid #E2E8F0;
            font-size: 11.5px;
            vertical-align: middle;
        }
        .text-right {
            text-align: right;
        }
        .text-center {
            text-align: center;
        }

        /* Badges */
        .badge {
            display: inline-block;
            padding: 3px 8px;
            border-radius: 3px;
            font-size: 9.5px;
            font-weight: 700;
            letter-spacing: 0.3px;
            text-transform: uppercase;
        }
        .badge-covered {
            background: #F1F5F9;
            color: #0F172A;
            border: 1px solid #94A3B8;
        }
        .badge-charge {
            background: #FEF2F2;
            color: #991B1B;
            border: 1px solid #FCA5A5;
        }

        /* Financial Calculation */
        .calculation-section {
            display: flex;
            justify-content: flex-end;
            margin-bottom: 24px;
        }
        .calc-table {
            width: 360px;
            border-collapse: collapse;
        }
        .calc-table td {
            padding: 6px 10px;
            border: none;
            font-size: 11.5px;
        }
        .calc-table .label {
            color: #475569;
        }
        .calc-table .val {
            text-align: right;
            font-weight: 600;
            font-family: 'Consolas', 'Courier New', monospace;
            color: #0F172A;
        }
        .calc-table .discount-row td {
            color: #0F172A;
            font-weight: 600;
        }
        .calc-table .grand-row {
            border-top: 2px solid #0F172A;
            border-bottom: 2px solid #0F172A;
            background: #F8FAFC;
        }
        .calc-table .grand-row td {
            padding: 10px;
            font-size: 13px;
            font-weight: 800;
        }
        .calc-table .grand-row .val {
            font-size: 14px;
            font-weight: 800;
        }

        .terbilang-box {
            background: #F8FAFC;
            border-left: 3px solid #0F172A;
            padding: 8px 14px;
            margin-bottom: 24px;
            font-size: 11px;
            color: #334155;
        }
        .terbilang-box strong {
            color: #0F172A;
            text-transform: uppercase;
            font-size: 10px;
            letter-spacing: 0.5px;
            display: block;
            margin-bottom: 2px;
        }

        /* Signatures & Seal Section */
        .signatures-section {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 30px;
            padding-top: 16px;
            border-top: 1px solid #CBD5E1;
            margin-bottom: 20px;
        }
        .sig-block {
            text-align: center;
        }
        .sig-role {
            font-size: 10.5px;
            font-weight: 700;
            color: #475569;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 8px;
        }
        .sig-space {
            height: 75px;
            display: flex;
            align-items: center;
            justify-content: center;
        }
        .sig-digital-stamp {
            border: 1.5px solid #0F172A;
            border-radius: 4px;
            padding: 8px 14px;
            background: #F8FAFC;
            display: inline-block;
            text-align: center;
        }
        .stamp-company {
            font-size: 9.5px;
            font-weight: 800;
            color: #0F172A;
            letter-spacing: 0.5px;
        }
        .stamp-verif {
            font-size: 11px;
            font-weight: 800;
            color: #0F172A;
            letter-spacing: 0.8px;
            margin: 2px 0;
            padding: 2px 0;
            border-top: 1px solid #CBD5E1;
            border-bottom: 1px solid #CBD5E1;
        }
        .stamp-meta {
            font-size: 8.5px;
            color: #64748B;
            font-family: 'Consolas', 'Courier New', monospace;
        }
        .sig-name {
            font-size: 11.5px;
            font-weight: 700;
            color: #0F172A;
            border-top: 1px solid #94A3B8;
            padding-top: 4px;
            display: inline-block;
            min-width: 200px;
        }
        .sig-sub {
            font-size: 10px;
            color: #64748B;
            margin-top: 2px;
        }

        /* Legal Terms */
        .legal-terms {
            border-top: 1px dashed #CBD5E1;
            padding-top: 14px;
            font-size: 10px;
            color: #64748B;
            line-height: 1.5;
        }
        .legal-terms-title {
            font-weight: 700;
            color: #334155;
            text-transform: uppercase;
            font-size: 9.5px;
            letter-spacing: 0.5px;
            margin-bottom: 4px;
        }
        .legal-terms ol {
            padding-left: 16px;
        }
        .legal-terms li {
            margin-bottom: 2px;
        }

        /* Action Toolbar (Screen only) */
        .action-toolbar {
            background: #0F172A;
            color: #FFFFFF;
            padding: 14px 30px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }
        .action-toolbar-info {
            font-size: 11.5px;
            color: #94A3B8;
        }
        .btn-print {
            background: #FFFFFF;
            color: #0F172A;
            border: none;
            padding: 8px 18px;
            border-radius: 4px;
            font-weight: 700;
            font-size: 12px;
            cursor: pointer;
            letter-spacing: 0.3px;
            transition: background-color 0.15s ease;
        }
        .btn-print:hover {
            background: #E2E8F0;
        }

        @media print {
            body {
                background: #FFFFFF;
                padding: 0;
            }
            .invoice-container {
                box-shadow: none;
                border: none;
                max-width: 100%;
            }
            .action-toolbar {
                display: none;
            }
        }
    </style>
</head>
<body>

<div class="invoice-container">
    <!-- Action Toolbar (Hidden during print) -->
    <div class="action-toolbar">
        <div class="action-toolbar-info">
            Dokumen Arsip Resmi • Sistem Garansi & Layanan Purna Jual PT JTS
        </div>
        <button class="btn-print" onclick="window.print()">Cetak Dokumen / Simpan PDF</button>
    </div>

    <!-- Kop Surat Resmi Perusahaan -->
    <div class="kop-header">
        <div class="kop-top">
            <div class="company-identity">
                <div class="company-brand-row">
                    <span class="company-logo-badge">PT JTS</span>
                    <span class="company-name">PT JAYA TEKNOLOGI SOLUSI</span>
                </div>
                <div class="company-dept">Divisi Layanan Purna Jual &amp; Jaminan Garansi Resmi Hardware</div>
                <div class="company-meta">
                    Gedung Cyber 2 Tower Lt. 18, Jl. H.R. Rasuna Said Blok X-5 No. 13, Jakarta Selatan 12950<br>
                    Telepon: (021) 5088-7799 • Email: aftersales@jts.co.id • NPWP: 01.884.223.4-015.000
                </div>
            </div>
            <div class="doc-identity">
                <div class="doc-title">FAKTUR SERVIS RESMI</div>
                <div class="doc-subtitle">BUKTI KLAIM GARANSI &amp; SUKU CADANG</div>
                <div class="doc-number">{{ $invoice->invoice_number }}</div>
                <div class="doc-date">Tanggal Terbit: {{ \Carbon\Carbon::parse($invoice->issue_date)->format('d F Y') }}</div>
            </div>
        </div>
    </div>

    <!-- Info Grid Pelanggan & Perangkat -->
    <div class="info-grid">
        <div class="info-block">
            <div class="info-title">IDENTITAS PELANGGAN &amp; PENERIMA</div>
            <div class="info-row">
                <span class="info-label">Nama Lengkap</span>
                <span class="info-value bold">: {{ $invoice->user->name ?? 'Pelanggan Terdaftar' }}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Kontak / Telepon</span>
                <span class="info-value">: {{ $invoice->user->phone ?? '-' }}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Email Terdaftar</span>
                <span class="info-value">: {{ $invoice->user->email ?? '-' }}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Alamat Penjemputan</span>
                <span class="info-value">: {{ $invoice->user->address ?? 'Alamat Terdaftar Sistem' }}</span>
            </div>
        </div>
        <div class="info-block">
            <div class="info-title">DATA PERANGKAT &amp; PENANGANAN</div>
            <div class="info-row">
                <span class="info-label">Model Perangkat</span>
                <span class="info-value bold">: {{ $invoice->device->model ?? '-' }}</span>
            </div>
            <div class="info-row">
                <span class="info-label">Nomor Seri (S/N)</span>
                <span class="info-value">: <code>{{ $invoice->device->serial_number ?? '-' }}</code></span>
            </div>
            <div class="info-row">
                <span class="info-label">Hardware / BIOS ID</span>
                <span class="info-value">: <code>{{ $invoice->device->hardware_id ?? '-' }}</code></span>
            </div>
            <div class="info-row">
                <span class="info-label">No. Tiket Perbaikan</span>
                <span class="info-value bold">: #{{ $invoice->repair_request_id }} (Tipe: {{ strtoupper($invoice->repairRequest->type ?? 'ON_SITE') }})</span>
            </div>
        </div>
    </div>

    <!-- Items Table -->
    <div class="content">
        <table>
            <thead>
                <tr>
                    <th style="width: 5%;" class="text-center">No</th>
                    <th style="width: 44%;">Deskripsi Suku Cadang / Tindakan Servis</th>
                    <th style="width: 18%;" class="text-center">Status Cakupan</th>
                    <th style="width: 6%;" class="text-center">Qty</th>
                    <th style="width: 13%;" class="text-right">Harga Satuan</th>
                    <th style="width: 14%;" class="text-right">Subtotal</th>
                </tr>
            </thead>
            <tbody>
                @foreach ($invoice->items as $idx => $item)
                <tr>
                    <td class="text-center" style="color: #64748B; font-weight: 600;">{{ $idx + 1 }}</td>
                    <td>
                        <div style="font-weight: 700; color: #0F172A;">{{ $item->item_name }}</div>
                        @if ($item->item_code)
                            <div style="font-size: 10.5px; color: #64748B; font-family: 'Consolas', monospace; margin-top: 1px;">Part Code: {{ $item->item_code }}</div>
                        @endif
                        @if ($item->notes)
                            <div style="font-size: 10.5px; color: #475569; margin-top: 2px;">Catatan: {{ $item->notes }}</div>
                        @endif
                    </td>
                    <td class="text-center">
                        @if ($item->is_covered_by_warranty)
                            <span class="badge badge-covered">DIJAMIN GARANSI</span>
                        @else
                            <span class="badge badge-charge">BIAYA PELANGGAN</span>
                        @endif
                    </td>
                    <td class="text-center" style="font-weight: 600;">{{ $item->quantity }}</td>
                    <td class="text-right" style="font-family: 'Consolas', monospace;">Rp {{ number_format($item->unit_price, 0, ',', '.') }}</td>
                    <td class="text-right" style="font-family: 'Consolas', monospace; font-weight: 600;">Rp {{ number_format($item->subtotal, 0, ',', '.') }}</td>
                </tr>
                @endforeach
            </tbody>
        </table>

        <!-- Summary Calculation -->
        <div class="calculation-section">
            <table class="calc-table">
                <tr>
                    <td class="label">Subtotal Nilai Pekerjaan:</td>
                    <td class="val">Rp {{ number_format($invoice->subtotal_amount, 0, ',', '.') }}</td>
                </tr>
                <tr class="discount-row">
                    <td class="label">Potongan Garansi Resmi PT JTS:</td>
                    <td class="val">- Rp {{ number_format($invoice->warranty_discount_amount, 0, ',', '.') }}</td>
                </tr>
                @if ($invoice->tax_amount > 0)
                <tr>
                    <td class="label">PPN (11%):</td>
                    <td class="val">Rp {{ number_format($invoice->tax_amount, 0, ',', '.') }}</td>
                </tr>
                @endif
                <tr class="grand-row">
                    <td>TOTAL TAGIHAN PELANGGAN:</td>
                    <td class="val">Rp {{ number_format($invoice->total_payable_amount, 0, ',', '.') }}</td>
                </tr>
            </table>
        </div>

        <!-- Terbilang Box -->
        <div class="terbilang-box">
            <strong>Keterangan Beban Pembayaran:</strong>
            @if ($invoice->total_payable_amount <= 0)
                Seluruh biaya suku cadang dan jasa teknisi sebesar Rp {{ number_format($invoice->subtotal_amount, 0, ',', '.') }} ditanggung penuh (100%) oleh fasilitas Garansi Resmi Hardware PT Jaya Teknologi Solusi. Pelanggan tidak dikenakan biaya apapun.
            @else
                Total kewajiban bayar sebesar Rp {{ number_format($invoice->total_payable_amount, 0, ',', '.') }} untuk penggantian komponen non-garansi / masa garansi berakhir.
            @endif
        </div>

        <!-- Signatures & Official Validation -->
        <div class="signatures-section">
            <div class="sig-block">
                <div class="sig-role">Penerima / Pemilik Unit</div>
                <div class="sig-space">
                    <span style="font-size: 10px; color: #94A3B8; font-style: italic;">(Tanda tangan saat serah terima unit)</span>
                </div>
                <div class="sig-name">{{ $invoice->user->name ?? 'Pelanggan Terdaftar' }}</div>
                <div class="sig-sub">Penerima Unit Komputer</div>
            </div>

            <div class="sig-block">
                <div class="sig-role">Pusat Layanan Purna Jual PT JTS</div>
                <div class="sig-space">
                    <div class="sig-digital-stamp">
                        <div class="stamp-company">PT JAYA TEKNOLOGI SOLUSI</div>
                        <div class="stamp-verif">PENGESAHAN ELEKTRONIK SAH</div>
                        <div class="stamp-meta">ID: JTS-SVC-{{ str_pad($invoice->id, 5, '0', STR_PAD_LEFT) }} • {{ \Carbon\Carbon::parse($invoice->issue_date)->format('Y-m-d') }}</div>
                    </div>
                </div>
                <div class="sig-name">{{ $invoice->technician->name ?? 'Tim Servis Resmi Hardware' }}</div>
                <div class="sig-sub">Petugas Teknisi Resmi Terverifikasi</div>
            </div>
        </div>

        <!-- Formal Terms & Conditions -->
        <div class="legal-terms">
            <div class="legal-terms-title">Ketentuan &amp; Jaminan Layanan Servis Resmi PT JTS:</div>
            <ol>
                <li>Suku cadang resmi (OEM) yang diganti dalam faktur ini memperoleh garansi pergantian suku cadang selama 1 (satu) tahun terhitung sejak tanggal faktur.</li>
                <li>Faktur elektronik ini merupakan dokumen sah serah terima dan bukti riwayat klaim garansi yang diakui oleh seluruh pusat servis jaringan PT Jaya Teknologi Solusi.</li>
                <li>Garansi tidak mencakup kerusakan fisik akibat bencana alam, kelalaian pengguna (terjatuh, terkena cairan), atau modifikasi perangkat oleh pihak ketiga tanpa persetujuan resmi.</li>
            </ol>
        </div>
    </div>
</div>

</body>
</html>
