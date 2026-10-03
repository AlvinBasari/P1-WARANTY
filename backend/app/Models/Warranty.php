<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Warranty extends Model
{
    use HasFactory;

    protected $fillable = [
        'device_id',
        'purchase_date',
        'warranty_start',
        'warranty_end',
        'status',
    ];

    protected $casts = [
        'purchase_date' => 'date',
        'warranty_start' => 'date',
        'warranty_end' => 'date',
    ];

    public function device()
    {
        return $this->belongsTo(Device::class);
    }

    public function isExpired(): bool
    {
        return now()->toDateString() > $this->warranty_end->toDateString();
    }
}
