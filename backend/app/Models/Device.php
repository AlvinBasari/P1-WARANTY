<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class Device extends Model
{
    use HasFactory;

    protected $fillable = [
        'hardware_id',
        'model',
        'serial_number',
        'user_id',
        'qr_token',
        'location_lat',
        'location_lng',
        'location_label',
    ];

    protected $casts = [
        'location_lat' => 'float',
        'location_lng' => 'float',
    ];

    public function user()
    {
        return $this->belongsTo(User::class);
    }

    public function warranty()
    {
        return $this->hasOne(Warranty::class);
    }

    public function repairRequests()
    {
        return $this->hasMany(RepairRequest::class);
    }
}
