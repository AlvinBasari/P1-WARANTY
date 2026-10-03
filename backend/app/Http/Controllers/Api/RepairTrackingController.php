<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\RepairRequest;
use App\Models\RepairTracking;
use App\Models\RepairTrackingHistory;
use Illuminate\Http\Request;

class RepairTrackingController extends Controller
{
    /**
     * Get tracking status and history for a repair request (FR-21).
     */
    public function show($requestId)
    {
        $repairRequest = RepairRequest::with(['device', 'user'])->findOrFail($requestId);

        $tracking = RepairTracking::with(['histories.user'])
            ->where('repair_request_id', $requestId)
            ->first();

        if (!$tracking) {
            return response()->json([
                'has_tracking' => false,
                'message' => 'Perangkat ini tidak sedang dalam alur perbaikan di kantor/service center.',
            ], 404);
        }

        return response()->json([
            'has_tracking' => true,
            'repair_request' => $repairRequest,
            'tracking' => $tracking,
        ]);
    }

    /**
     * Admin/Technician updates tracking stage and adds timeline note (FR-20).
     */
    public function updateProgress(Request $request, $requestId)
    {
        $repairRequest = RepairRequest::findOrFail($requestId);

        $validated = $request->validate([
            'status' => 'required|in:dijemput,di_service_center,sedang_diperbaiki,selesai,dikembalikan',
            'notes' => 'nullable|string',
        ]);

        $tracking = RepairTracking::firstOrCreate(
            ['repair_request_id' => $repairRequest->id],
            ['current_status' => $validated['status']]
        );

        $tracking->current_status = $validated['status'];
        $tracking->save();

        // Record history log
        $history = RepairTrackingHistory::create([
            'repair_tracking_id' => $tracking->id,
            'status' => $validated['status'],
            'changed_by' => $request->user()->id,
            'notes' => $validated['notes'] ?? 'Status perbaikan diperbarui ke: ' . $validated['status'],
        ]);

        if ($validated['status'] === 'dikembalikan') {
            $repairRequest->status = 'completed';
            $repairRequest->save();
        }

        return response()->json([
            'message' => 'Status progres perbaikan berhasil diperbarui',
            'tracking' => $tracking->load('histories.user'),
        ]);
    }
}
