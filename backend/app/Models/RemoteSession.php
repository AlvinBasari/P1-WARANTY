<?php

namespace App\Models;

use Illuminate\Database\Eloquent\Factories\HasFactory;
use Illuminate\Database\Eloquent\Model;

class RemoteSession extends Model
{
    use HasFactory;

    protected $fillable = [
        'repair_request_id',
        'technician_id',
        'rustdesk_session_id',
        'scheduled_at',
        'started_at',
        'ended_at',
        'connection_status',
        'notes',
    ];

    protected $casts = [
        'scheduled_at' => 'datetime',
        'started_at' => 'datetime',
        'ended_at' => 'datetime',
    ];

    public function repairRequest()
    {
        return $this->belongsTo(RepairRequest::class);
    }

    public function technician()
    {
        return $this->belongsTo(User::class, 'technician_id');
    }
}
