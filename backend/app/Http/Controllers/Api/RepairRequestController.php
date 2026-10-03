<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Device;
use App\Models\User;
use App\Models\RepairRequest;
use App\Models\RemoteSession;
use App\Models\RepairTracking;
use App\Models\RepairTrackingHistory;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Storage;

class RepairRequestController extends Controller
{
    /**
     * List repair requests with filtering.
     */
    public function index(Request $request)
    {
        $user = $request->user();

        $query = RepairRequest::with(['device.warranty', 'user', 'remoteSession', 'tracking.histories.user'])
            ->latest();

        // Customer sees only their own requests
        if ($user->isCustomer()) {
            $query->where('user_id', $user->id);
        }

        // Filters
        if ($request->filled('type')) {
            $query->where('type', $request->type);
        }

        if ($request->filled('damage_category')) {
            $query->where('damage_category', $request->damage_category);
        }

        if ($request->filled('status')) {
            $query->where('status', $request->status);
        }

        if ($request->boolean('unconfirmed_location_only')) {
            $query->where('type', 'on_site')->where('location_confirmed_by_admin', false);
        }

        $requests = $query->paginate(20);

        return response()->json($requests);
    }

    /**
     * Show single repair request.
     */
    public function show($id)
    {
        $repairRequest = RepairRequest::with([
            'device.warranty',
            'user',
            'remoteSession.technician',
            'tracking.histories.user'
        ])->findOrFail($id);

        return response()->json([
            'request' => $repairRequest,
        ]);
    }

    /**
     * Create a repair request from Desktop App.
     */
    public function store(Request $request)
    {
        $validated = $request->validate([
            'device_id' => 'required|exists:devices,id',
            'damage_category' => 'required|in:physical,non_physical',
            'description' => 'required|string|min:5',
            'preferred_schedule' => 'nullable|string|max:255',
            'rustdesk_session_id' => 'nullable|string',
            'attachments' => 'nullable|array',
            'attachments.*' => 'string',
        ]);

        $device = Device::findOrFail($validated['device_id']);
        $user = $request->user();

        // FR-16 & FR-17: Determine request type based on damage category
        $isPhysical = ($validated['damage_category'] === 'physical');
        $type = $isPhysical ? 'on_site' : 'remote';

        // FR-14: Attach pre-calibrated location automatically without re-asking
        $locationLat = $device->location_lat;
        $locationLng = $device->location_lng;

        $repairRequest = RepairRequest::create([
            'device_id' => $device->id,
            'user_id' => $user->id,
            'damage_category' => $validated['damage_category'],
            'type' => $type,
            'description' => $validated['description'],
            'preferred_schedule' => $validated['preferred_schedule'] ?? null,
            'attachments' => $validated['attachments'] ?? [],
            'location_lat' => $locationLat,
            'location_lng' => $locationLng,
            'location_confirmed_by_admin' => false,
            'needs_office_repair' => false,
            'status' => 'pending',
        ]);

        // FR-04: If remote request, generate and attach RemoteSession (pending technician acceptance)
        if ($type === 'remote' && !empty($validated['rustdesk_session_id'])) {
            RemoteSession::create([
                'repair_request_id' => $repairRequest->id,
                'rustdesk_session_id' => $validated['rustdesk_session_id'],
                'connection_status' => 'waiting_acceptance',
                'notes' => 'Permintaan diajukan. Menunggu peninjauan kendala dan penetapan jadwal resmi oleh teknisi.',
            ]);
        }

        return response()->json([
            'message' => 'Permintaan perbaikan berhasil dikirim dan masuk antrean peninjauan teknisi',
            'request' => $repairRequest->load(['device', 'remoteSession', 'tracking']),
        ], 201);
    }

    /**
     * Upload attachment file (image/video).
     */
    public function uploadAttachment(Request $request)
    {
        $request->validate([
            'file' => 'required|file|mimes:jpeg,png,jpg,webp,mp4,mov,avi|max:20480', // max 20MB
        ]);

        $path = $request->file('file')->store('attachments', 'public');

        return response()->json([
            'url' => Storage::disk('public')->url($path),
            'filename' => basename($path),
        ]);
    }

    /**
     * Admin confirms / adjusts on-site location (FR-15).
     */
    public function confirmLocation(Request $request, $id)
    {
        $repairRequest = RepairRequest::findOrFail($id);

        $validated = $request->validate([
            'location_lat' => 'nullable|numeric',
            'location_lng' => 'nullable|numeric',
            'admin_notes' => 'nullable|string',
        ]);

        if (isset($validated['location_lat']) && isset($validated['location_lng'])) {
            $repairRequest->location_lat = $validated['location_lat'];
            $repairRequest->location_lng = $validated['location_lng'];
        }

        $repairRequest->location_confirmed_by_admin = true;
        $repairRequest->status = 'in_progress';
        $repairRequest->save();

        return response()->json([
            'message' => 'Lokasi servis on-site berhasil dikonfirmasi',
            'request' => $repairRequest->load(['device', 'user']),
        ]);
    }

    /**
     * Technician / Admin accepts request and schedules remote session.
     */
    public function scheduleRemote(Request $request, $id)
    {
        $repairRequest = RepairRequest::with(['remoteSession', 'device', 'user'])->findOrFail($id);

        $validated = $request->validate([
            'scheduled_at' => 'required|string',
            'technician_id' => 'nullable|exists:users,id',
            'notes' => 'nullable|string',
        ]);

        $tech = User::where('role', 'technician')->first();
        $techId = $validated['technician_id'] ?? ($tech ? $tech->id : $request->user()->id);

        $repairRequest->status = 'scheduled';
        $repairRequest->scheduled_at = $validated['scheduled_at'];
        $repairRequest->save();

        if ($repairRequest->remoteSession) {
            $repairRequest->remoteSession->update([
                'technician_id' => $techId,
                'scheduled_at' => $validated['scheduled_at'],
                'connection_status' => 'scheduled',
                'notes' => $validated['notes'] ?? 'Permintaan telah disetujui. Sesi remote dijadwalkan pada ' . $validated['scheduled_at'],
            ]);
        }

        return response()->json([
            'message' => 'Permintaan perbaikan jarak jauh berhasil disetujui dan dijadwalkan',
            'request' => $repairRequest->fresh(['device', 'remoteSession.technician', 'user']),
        ]);
    }

    /**
     * Update request overall status (Admin/Technician).
     */
    public function updateStatus(Request $request, $id)
    {
        $repairRequest = RepairRequest::findOrFail($id);

        $validated = $request->validate([
            'status' => 'required|in:pending,in_progress,completed,rejected,cancelled',
        ]);

        $repairRequest->update(['status' => $validated['status']]);

        return response()->json([
            'message' => 'Status permintaan berhasil diperbarui',
            'request' => $repairRequest,
        ]);
    }

    /**
     * Technician marks on-site inspection result: finished on-site OR needs office repair (FR-18, FR-19).
     */
    public function markOfficeRepair(Request $request, $id)
    {
        $repairRequest = RepairRequest::findOrFail($id);

        $validated = $request->validate([
            'needs_office_repair' => 'required|boolean',
            'notes' => 'nullable|string',
        ]);

        $needsOfficeRepair = $validated['needs_office_repair'];
        $repairRequest->needs_office_repair = $needsOfficeRepair;

        if ($needsOfficeRepair) {
            $repairRequest->status = 'in_progress';
            $repairRequest->save();

            // Create tracking record if not exists
            $tracking = RepairTracking::firstOrCreate(
                ['repair_request_id' => $repairRequest->id],
                ['current_status' => 'dijemput']
            );

            // Record history
            RepairTrackingHistory::create([
                'repair_tracking_id' => $tracking->id,
                'status' => 'dijemput',
                'changed_by' => $request->user()->id,
                'notes' => $validated['notes'] ?? 'Unit tidak dapat diperbaiki di tempat dan dibawa ke service center.',
            ]);
        } else {
            $repairRequest->status = 'completed';
            $repairRequest->save();
        }

        return response()->json([
            'message' => $needsOfficeRepair ? 'Status ditandai: Perlu dibawa ke service center' : 'Status ditandai: Selesai di tempat',
            'request' => $repairRequest->load(['device', 'tracking.histories']),
        ]);
    }
}
