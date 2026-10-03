<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Device;
use App\Models\RepairRequest;
use App\Models\User;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;

class QrClaimController extends Controller
{
    /**
     * Public lookup device and warranty info by QR Token (FR-07).
     */
    public function lookup($token)
    {
        $device = Device::with(['warranty'])
            ->where('qr_token', $token)
            ->first();

        if (!$device) {
            return response()->json([
                'found' => false,
                'message' => 'QR Code tidak valid atau perangkat tidak terdaftar.',
            ], 404);
        }

        return response()->json([
            'found' => true,
            'device' => [
                'model' => $device->model,
                'serial_number' => $device->serial_number,
                'qr_token' => $device->qr_token,
                'location_lat' => $device->location_lat,
                'location_lng' => $device->location_lng,
                'location_label' => $device->location_label,
            ],
            'warranty' => $device->warranty,
        ]);
    }

    /**
     * Public submit damage report via QR Code (FR-08).
     */
    public function submitClaim(Request $request, $token)
    {
        $device = Device::where('qr_token', $token)->first();

        if (!$device) {
            return response()->json([
                'message' => 'QR Code tidak valid.',
            ], 404);
        }

        $validated = $request->validate([
            'name' => 'required|string|max:255',
            'phone' => 'required|string|max:20',
            'email' => 'nullable|email|max:255',
            'address' => 'required|string',
            'description' => 'required|string|min:10',
            'attachments' => 'nullable|array',
            'attachments.*' => 'string',
            'location_lat' => 'nullable|numeric',
            'location_lng' => 'nullable|numeric',
        ]);

        // Find or create customer account
        $user = null;
        if (!empty($validated['email'])) {
            $user = User::where('email', $validated['email'])->first();
        }

        if (!$user) {
            $email = $validated['email'] ?? 'qr_user_' . Str::random(6) . '@warranty-temp.com';
            $user = User::create([
                'name' => $validated['name'],
                'email' => $email,
                'phone' => $validated['phone'],
                'address' => $validated['address'],
                'role' => 'customer',
                'password' => Hash::make(Str::random(12)),
            ]);
        }

        // Bind device to user if not bound
        if (!$device->user_id) {
            $device->user_id = $user->id;
            if (!empty($validated['location_lat']) && !empty($validated['location_lng'])) {
                $device->location_lat = $validated['location_lat'];
                $device->location_lng = $validated['location_lng'];
                $device->location_label = $validated['address'];
            }
            $device->save();
        }

        $lat = $validated['location_lat'] ?? $device->location_lat;
        $lng = $validated['location_lng'] ?? $device->location_lng;

        $repairRequest = RepairRequest::create([
            'device_id' => $device->id,
            'user_id' => $user->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => "[Laporan Klaim via QR Code Perangkat]\n" . $validated['description'] . "\n\nAlamat Penjemputan: " . $validated['address'] . " (Telp: " . $validated['phone'] . ")",
            'attachments' => $validated['attachments'] ?? [],
            'location_lat' => $lat,
            'location_lng' => $lng,
            'location_confirmed_by_admin' => false,
            'needs_office_repair' => false,
            'status' => 'pending',
        ]);

        return response()->json([
            'message' => 'Laporan kerusakan via QR Code berhasil dikirimkan. Tim kami akan segera menindaklanjuti.',
            'request_id' => $repairRequest->id,
            'device_model' => $device->model,
        ], 201);
    }
}
