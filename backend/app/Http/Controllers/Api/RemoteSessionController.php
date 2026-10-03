<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\RemoteSession;
use App\Models\RepairRequest;
use Illuminate\Http\Request;

class RemoteSessionController extends Controller
{
    /**
     * List all remote sessions (Audit log).
     */
    public function index(Request $request)
    {
        $sessions = RemoteSession::with([
            'repairRequest.device.warranty',
            'repairRequest.user',
            'technician'
        ])->latest()->paginate(20);

        return response()->json($sessions);
    }

    /**
     * Start/Connect to a remote session.
     */
    public function startSession(Request $request, $id)
    {
        $session = RemoteSession::findOrFail($id);

        $session->technician_id = $request->user()->id;
        $session->started_at = now();
        $session->connection_status = 'connected';
        $session->save();

        // Update parent request status
        $session->repairRequest()->update(['status' => 'in_progress']);

        return response()->json([
            'message' => 'Sesi remote dimulai',
            'session' => $session->load(['repairRequest.device', 'technician']),
        ]);
    }

    /**
     * End a remote session and save diagnosis notes.
     */
    public function endSession(Request $request, $id)
    {
        $session = RemoteSession::findOrFail($id);

        $validated = $request->validate([
            'connection_status' => 'required|in:completed,failed',
            'notes' => 'nullable|string',
        ]);

        $session->ended_at = now();
        $session->connection_status = $validated['connection_status'];
        if (!empty($validated['notes'])) {
            $session->notes = $validated['notes'];
        }
        $session->save();

        if ($validated['connection_status'] === 'completed') {
            $session->repairRequest()->update(['status' => 'completed']);
        }

        return response()->json([
            'message' => 'Sesi remote selesai dan log telah dicatat',
            'session' => $session->load(['repairRequest.device', 'technician']),
        ]);
    }
}
