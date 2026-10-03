<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Device;
use App\Models\Warranty;
use Illuminate\Http\Request;
use Illuminate\Support\Str;

class DeviceController extends Controller
{
    /**
     * List devices.
     */
    public function index(Request $request)
    {
        $user = $request->user();

        if ($user->isAdmin() || $user->isTechnician()) {
            $devices = Device::with(['user', 'warranty', 'repairRequests.tracking.histories', 'repairRequests.remoteSession'])->latest()->get();
        } else {
            $devices = Device::with(['warranty', 'repairRequests.tracking.histories', 'repairRequests.remoteSession'])->where('user_id', $user->id)->get();
        }

        return response()->json([
            'devices' => $devices,
        ]);
    }

    /**
     * Lookup device info by Hardware/BIOS ID (used on desktop startup).
     */
    public function lookupByHardwareId($hardwareId)
    {
        $device = Device::with(['warranty', 'user', 'repairRequests.tracking.histories', 'repairRequests.remoteSession'])->where('hardware_id', $hardwareId)->first();

        if (!$device) {
            return response()->json([
                'found' => false,
                'message' => 'Perangkat belum terdaftar dalam sistem garansi PT JTS',
            ], 404);
        }

        return response()->json([
            'found' => true,
            'device' => $device,
        ]);
    }

    /**
     * Claim / bind pre-registered device using QR Token or Serial Number from the sticker.
     */
    public function claimByToken(Request $request)
    {
        $validated = $request->validate([
            'token_or_serial' => 'required|string',
            'hardware_id' => 'required|string',
            'model' => 'nullable|string',
        ]);

        $query = trim($validated['token_or_serial']);

        // Find device by qr_token or serial_number (pre-registered by Admin)
        $device = Device::where('qr_token', $query)
            ->orWhere('serial_number', $query)
            ->first();

        if (!$device) {
            return response()->json([
                'success' => false,
                'message' => 'Nomor Seri / Token QR tidak ditemukan dalam master data garansi PT JTS.',
            ], 404);
        }

        // Bind this physical hardware ID & current user to this pre-registered device
        $device->hardware_id = $validated['hardware_id'];
        $device->user_id = $request->user()->id;
        if (!empty($validated['model'])) {
            $device->model = $validated['model'];
        }
        $device->save();

        return response()->json([
            'success' => true,
            'message' => 'Perangkat dan garansi resmi PT JTS berhasil dihubungkan!',
            'device' => $device->load(['warranty', 'repairRequests.tracking.histories', 'repairRequests.remoteSession']),
        ]);
    }

    /**
     * Calibrate/save device location once (FR-13).
     */
    public function calibrateLocation(Request $request, $id)
    {
        $device = Device::findOrFail($id);

        // Ensure authorization
        if (!$request->user()->isAdmin() && $device->user_id !== $request->user()->id) {
            return response()->json(['message' => 'Tidak memiliki izin akses perangkat ini'], 403);
        }

        $validated = $request->validate([
            'location_lat' => 'required|numeric',
            'location_lng' => 'required|numeric',
            'location_label' => 'nullable|string|max:1000',
        ]);

        $device->update($validated);

        return response()->json([
            'message' => 'Lokasi perangkat berhasil dikalibrasi dan disimpan',
            'device' => $device,
        ]);
    }

    /**
     * Store a new device (Admin).
     */
    public function store(Request $request)
    {
        $validated = $request->validate([
            'hardware_id' => 'required|string|unique:devices',
            'model' => 'required|string',
            'serial_number' => 'required|string|unique:devices',
            'user_id' => 'nullable|exists:users,id',
            'location_lat' => 'nullable|numeric',
            'location_lng' => 'nullable|numeric',
            'location_label' => 'nullable|string',
            'purchase_date' => 'required|date',
            'warranty_years' => 'nullable|integer|min:1|max:5',
        ]);

        $device = Device::create([
            'hardware_id' => $validated['hardware_id'],
            'model' => $validated['model'],
            'serial_number' => $validated['serial_number'],
            'user_id' => $validated['user_id'] ?? null,
            'qr_token' => 'qr-' . Str::uuid(),
            'location_lat' => $validated['location_lat'] ?? null,
            'location_lng' => $validated['location_lng'] ?? null,
            'location_label' => $validated['location_label'] ?? null,
        ]);

        $years = $validated['warranty_years'] ?? 2;
        $purchaseDate = $validated['purchase_date'];
        $warrantyStart = $purchaseDate;
        $warrantyEnd = date('Y-m-d', strtotime("+$years years", strtotime($purchaseDate)));

        $warranty = Warranty::create([
            'device_id' => $device->id,
            'purchase_date' => $purchaseDate,
            'warranty_start' => $warrantyStart,
            'warranty_end' => $warrantyEnd,
            'status' => 'active',
        ]);

        return response()->json([
            'message' => 'Perangkat & garansi berhasil didaftarkan',
            'device' => $device->load('warranty'),
        ], 201);
    }

    /**
     * Show single device.
     */
    public function show($id)
    {
        $device = Device::with(['user', 'warranty', 'repairRequests.tracking', 'repairRequests.remoteSession'])->findOrFail($id);

        return response()->json([
            'device' => $device,
        ]);
    }
}
