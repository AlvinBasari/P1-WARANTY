<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Warranty;
use App\Models\Device;
use Illuminate\Http\Request;

class WarrantyController extends Controller
{
    /**
     * List all warranties.
     */
    public function index(Request $request)
    {
        $warranties = Warranty::with(['device.user'])->latest()->get();

        return response()->json([
            'warranties' => $warranties,
        ]);
    }

    /**
     * Check warranty status by hardware ID.
     */
    public function checkStatus($hardwareId)
    {
        $device = Device::where('hardware_id', $hardwareId)->first();

        if (!$device || !$device->warranty) {
            return response()->json([
                'status' => 'not_found',
                'message' => 'Data garansi tidak ditemukan',
            ], 404);
        }

        $warranty = $device->warranty;
        $isExpired = now()->toDateString() > $warranty->warranty_end->toDateString();

        return response()->json([
            'status' => $isExpired ? 'expired' : $warranty->status,
            'is_active' => !$isExpired && $warranty->status === 'active',
            'device' => $device,
            'warranty' => $warranty,
        ]);
    }

    /**
     * Update warranty status (Admin).
     */
    public function update(Request $request, $id)
    {
        $warranty = Warranty::findOrFail($id);

        $validated = $request->validate([
            'status' => 'required|in:active,expired,void',
            'warranty_end' => 'sometimes|required|date',
        ]);

        $warranty->update($validated);

        return response()->json([
            'message' => 'Status garansi berhasil diperbarui',
            'warranty' => $warranty->load('device'),
        ]);
    }
}
