<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class RepairRequest extends Model
{
    use HasFactory;

    protected $fillable = [
        'device_id',
        'user_id',
        'damage_category',
        'type',
        'description',
        'attachments',
        'location_lat',
        'location_lng',
        'location_confirmed_by_admin',
        'needs_office_repair',
        'preferred_schedule',
        'scheduled_at',
        'status',
    ];

    protected $casts = [
        'attachments' => 'array',
        'location_lat' => 'float',
        'location_lng' => 'float',
        'location_confirmed_by_admin' => 'boolean',
        'needs_office_repair' => 'boolean',
        'scheduled_at' => 'datetime',
    ];

    public function device()
    {
        return $this->belongsTo(Device::class);
    }

    public function user()
    {
        return $this->belongsTo(User::class);
    }

    public function remoteSession()
    {
        return $this->hasOne(RemoteSession::class);
    }

    public function tracking()
    {
        return $this->hasOne(RepairTracking::class);
    }

    public function invoice()
    {
        return $this->hasOne(Invoice::class);
    }
}
