<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class InvoiceItem extends Model
{
    use HasFactory;

    protected $fillable = [
        'invoice_id',
        'item_name',
        'item_code',
        'category',
        'quantity',
        'unit_price',
        'subtotal',
        'is_covered_by_warranty',
        'warranty_coverage_amount',
        'customer_payable_amount',
        'notes',
    ];

    protected $casts = [
        'quantity' => 'integer',
        'unit_price' => 'decimal:2',
        'subtotal' => 'decimal:2',
        'is_covered_by_warranty' => 'boolean',
        'warranty_coverage_amount' => 'decimal:2',
        'customer_payable_amount' => 'decimal:2',
    ];

    public function invoice()
    {
        return $this->belongsTo(Invoice::class);
    }
}
