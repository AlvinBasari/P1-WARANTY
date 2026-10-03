<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Invoice;
use App\Models\InvoiceItem;
use App\Models\RepairRequest;
use App\Models\User;
use Illuminate\Http\Request;
use Illuminate\Support\Str;

class InvoiceController extends Controller
{
    /**
     * List all invoices (with customer scoping).
     */
    public function index(Request $request)
    {
        $user = $request->user();
        $query = Invoice::with(['items', 'device.warranty', 'user', 'technician', 'repairRequest'])
            ->latest();

        if ($user->isCustomer()) {
            $query->where('user_id', $user->id);
        }

        if ($request->filled('payment_status')) {
            $query->where('payment_status', $request->payment_status);
        }

        $invoices = $query->paginate(20);

        return response()->json($invoices);
    }

    /**
     * Show single invoice details.
     */
    public function show($id, Request $request)
    {
        $user = $request->user();
        $invoice = Invoice::with(['items', 'device.warranty', 'user', 'technician', 'repairRequest'])
            ->findOrFail($id);

        if ($user->isCustomer() && $invoice->user_id !== $user->id) {
            return response()->json(['message' => 'Unauthorized to view this invoice.'], 403);
        }

        return response()->json([
            'invoice' => $invoice,
        ]);
    }

    /**
     * Get invoice for a specific repair request.
     */
    public function getByRepairRequest($requestId, Request $request)
    {
        $user = $request->user();
        $repairRequest = RepairRequest::findOrFail($requestId);

        if ($user->isCustomer() && $repairRequest->user_id !== $user->id) {
            return response()->json(['message' => 'Unauthorized to view this invoice.'], 403);
        }

        $invoice = Invoice::with(['items', 'device.warranty', 'user', 'technician', 'repairRequest'])
            ->where('repair_request_id', $requestId)
            ->first();

        if (!$invoice) {
            return response()->json([
                'has_invoice' => false,
                'message' => 'Invoice perbaikan belum diterbitkan untuk tiket ini.',
            ], 404);
        }

        return response()->json([
            'has_invoice' => true,
            'invoice' => $invoice,
        ]);
    }

    /**
     * Create or update invoice for a repair request.
     */
    public function storeOrUpdate(Request $request, $requestId)
    {
        $repairRequest = RepairRequest::with(['device.warranty', 'user'])->findOrFail($requestId);
        $user = $request->user();

        $validated = $request->validate([
            'technician_id' => 'nullable|exists:users,id',
            'notes' => 'nullable|string',
            'terms_and_conditions' => 'nullable|string',
            'payment_status' => 'nullable|in:paid_by_warranty,paid,unpaid,partially_paid,cancelled',
            'payment_method' => 'nullable|string',
            'items' => 'required|array|min:1',
            'items.*.item_name' => 'required|string',
            'items.*.item_code' => 'nullable|string',
            'items.*.category' => 'required|in:sparepart,service_fee,diagnostic_fee,transport_fee,other',
            'items.*.quantity' => 'required|integer|min:1',
            'items.*.unit_price' => 'required|numeric|min:0',
            'items.*.is_covered_by_warranty' => 'required|boolean',
            'items.*.notes' => 'nullable|string',
        ]);

        $techId = $validated['technician_id'] ?? $user->id;

        // Find or create invoice
        $invoice = Invoice::firstOrNew(['repair_request_id' => $repairRequest->id]);

        if (!$invoice->exists) {
            $datePrefix = date('Ym');
            $randomSuffix = str_pad((string) mt_rand(1000, 9999), 4, '0', STR_PAD_LEFT);
            $invoice->invoice_number = "INV-{$datePrefix}-{$repairRequest->id}{$randomSuffix}";
            $invoice->issue_date = now();
            $invoice->due_date = now()->addDays(7);
        }

        $invoice->device_id = $repairRequest->device_id;
        $invoice->user_id = $repairRequest->user_id;
        $invoice->technician_id = $techId;
        $invoice->notes = $validated['notes'] ?? 'Faktur resmi jaminan garansi dan perbaikan perangkat PT JTS.';
        $invoice->terms_and_conditions = $validated['terms_and_conditions'] ?? '1. Suku cadang resmi digaransi selama 1 tahun.\n2. Biaya yang dijamin garansi resmi telah dipotong 100%.\n3. Harap simpan faktur ini sebagai bukti klaim sah.';
        $invoice->save();

        // Sync items
        $invoice->items()->delete();

        foreach ($validated['items'] as $itemData) {
            $qty = $itemData['quantity'];
            $unitPrice = $itemData['unit_price'];
            $subtotal = $qty * $unitPrice;
            $isCovered = (bool)$itemData['is_covered_by_warranty'];

            $warrantyCoverage = $isCovered ? $subtotal : 0;
            $customerPayable = $isCovered ? 0 : $subtotal;

            InvoiceItem::create([
                'invoice_id' => $invoice->id,
                'item_name' => $itemData['item_name'],
                'item_code' => $itemData['item_code'] ?? null,
                'category' => $itemData['category'],
                'quantity' => $qty,
                'unit_price' => $unitPrice,
                'subtotal' => $subtotal,
                'is_covered_by_warranty' => $isCovered,
                'warranty_coverage_amount' => $warrantyCoverage,
                'customer_payable_amount' => $customerPayable,
                'notes' => $itemData['notes'] ?? null,
            ]);
        }

        // Recalculate totals
        $invoice->refresh();
        $invoice->recalculateTotals();

        if (isset($validated['payment_status'])) {
            $invoice->payment_status = $validated['payment_status'];
        }
        if (isset($validated['payment_method'])) {
            $invoice->payment_method = $validated['payment_method'];
        }
        $invoice->save();

        return response()->json([
            'message' => 'Invoice perbaikan berhasil dibuat & disinkronkan',
            'invoice' => $invoice->load(['items', 'device.warranty', 'user', 'technician', 'repairRequest']),
        ]);
    }

    /**
     * Download printable/viewable HTML invoice.
     */
    public function downloadInvoiceHtml($id, Request $request)
    {
        $invoice = Invoice::with(['items', 'device.warranty', 'user', 'technician', 'repairRequest'])
            ->findOrFail($id);

        $user = $request->user();
        if ($user && $user->isCustomer() && $invoice->user_id !== $user->id) {
            return response()->json(['message' => 'Unauthorized.'], 403);
        }

        $html = view('invoices.print', ['invoice' => $invoice])->render();

        return response($html, 200)
            ->header('Content-Type', 'text/html');
    }
}
