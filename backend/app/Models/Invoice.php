<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Invoice extends Model
{
    use HasFactory;

    protected $fillable = [
        'invoice_number',
        'repair_request_id',
        'device_id',
        'user_id',
        'technician_id',
        'issue_date',
        'due_date',
        'subtotal_amount',
        'warranty_discount_amount',
        'tax_amount',
        'total_payable_amount',
        'payment_status',
        'payment_method',
        'notes',
        'terms_and_conditions',
    ];

    protected $casts = [
        'issue_date' => 'datetime',
        'due_date' => 'datetime',
        'subtotal_amount' => 'decimal:2',
        'warranty_discount_amount' => 'decimal:2',
        'tax_amount' => 'decimal:2',
        'total_payable_amount' => 'decimal:2',
    ];

    public function repairRequest()
    {
        return $this->belongsTo(RepairRequest::class);
    }

    public function device()
    {
        return $this->belongsTo(Device::class);
    }

    public function user()
    {
        return $this->belongsTo(User::class);
    }

    public function technician()
    {
        return $this->belongsTo(User::class, 'technician_id');
    }

    public function items()
    {
        return $this->hasMany(InvoiceItem::class);
    }

    /**
     * Recalculate totals from items.
     */
    public function recalculateTotals(): void
    {
        $subtotal = 0;
        $discount = 0;
        $payable = 0;

        foreach ($this->items as $item) {
            $itemSubtotal = $item->unit_price * $item->quantity;
            $subtotal += $itemSubtotal;

            if ($item->is_covered_by_warranty) {
                $itemWarrantyCoverage = $itemSubtotal;
                $itemPayable = 0;
            } else {
                $itemWarrantyCoverage = 0;
                $itemPayable = $itemSubtotal;
            }

            $item->subtotal = $itemSubtotal;
            $item->warranty_coverage_amount = $itemWarrantyCoverage;
            $item->customer_payable_amount = $itemPayable;
            $item->save();

            $discount += $itemWarrantyCoverage;
            $payable += $itemPayable;
        }

        $this->subtotal_amount = $subtotal;
        $this->warranty_discount_amount = $discount;
        $this->total_payable_amount = $payable + ($this->tax_amount ?? 0);

        if ($this->total_payable_amount <= 0) {
            $this->payment_status = 'paid_by_warranty';
            $this->payment_method = 'Jaminan Garansi Resmi PT JTS';
        } else if ($this->payment_status === 'paid_by_warranty') {
            $this->payment_status = 'unpaid';
        }

        $this->save();
    }
}
