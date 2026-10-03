<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class RepairTrackingHistory extends Model
{
    use HasFactory;

    protected $fillable = [
        'repair_tracking_id',
        'status',
        'changed_by',
        'notes',
    ];

    public function repairTracking()
    {
        return $this->belongsTo(RepairTracking::class);
    }

    public function user()
    {
        return $this->belongsTo(User::class, 'changed_by');
    }
}
