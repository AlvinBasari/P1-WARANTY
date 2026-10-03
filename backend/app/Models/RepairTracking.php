<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class RepairTracking extends Model
{
    use HasFactory;

    protected $fillable = [
        'repair_request_id',
        'current_status',
    ];

    public function repairRequest()
    {
        return $this->belongsTo(RepairRequest::class);
    }

    public function histories()
    {
        return $this->hasMany(RepairTrackingHistory::class)->orderBy('created_at', 'asc');
    }
}
