<?php

namespace App\Http\Controllers\Api;

use App\Http\Controllers\Controller;
use App\Models\Device;
use App\Models\User;
use App\Models\Warranty;
use Illuminate\Http\Request;
use Illuminate\Support\Facades\Hash;
use Illuminate\Support\Str;
use Illuminate\Validation\ValidationException;

class AuthController extends Controller
{
    /**
     * Register a new user.
     */
    public function register(Request $request)
    {
        $validated = $request->validate([
            'name' => 'required|string|max:255',
            'email' => 'required|string|email|max:255|unique:users',
            'password' => 'required|string|min:6',
            'phone' => 'nullable|string|max:20',
            'address' => 'nullable|string',
            'role' => 'nullable|string|in:customer,technician,admin',
            'hardware_id' => 'nullable|string', // BIOS UUID / Motherboard Serial
        ]);

        $user = User::create([
            'name' => $validated['name'],
            'email' => $validated['email'],
            'password' => Hash::make($validated['password']),
            'phone' => $validated['phone'] ?? null,
            'address' => $validated['address'] ?? null,
            'role' => $validated['role'] ?? 'customer',
        ]);

        $activeDevice = null;

        // Check if this physical PC was pre-registered in JTS Master Data
        if (!empty($validated['hardware_id'])) {
            $device = Device::where('hardware_id', $validated['hardware_id'])->first();

            if ($device) {
                if (is_null($device->user_id) || $device->user_id === $user->id) {
                    $device->user_id = $user->id;
                    $device->save();
                }
                $activeDevice = $device->load('warranty');
            }
        }

        $token = $user->createToken('auth_token')->plainTextToken;

        return response()->json([
            'message' => 'Registrasi berhasil',
            'token' => $token,
            'user' => $user->load('devices.warranty'),
            'active_device' => $activeDevice,
            'linked_device' => $activeDevice,
        ], 201);
    }

    /**
     * Login user with strictly verified hardware matching.
     */
    public function login(Request $request)
    {
        $request->validate([
            'email' => 'required|email',
            'password' => 'required',
            'hardware_id' => 'nullable|string',
        ]);

        $user = User::where('email', $request->email)->first();

        if (!$user || !Hash::check($request->password, $user->password)) {
            throw ValidationException::withMessages([
                'email' => ['Kredensial yang diberikan tidak cocok dengan data kami.'],
            ]);
        }

        $activeDevice = null;

        // Check if this physical machine's Hardware ID exists in official JTS database
        if ($request->filled('hardware_id')) {
            $device = Device::where('hardware_id', $request->hardware_id)->first();
            if ($device) {
                // If unassigned or belongs to this user, link it
                if (is_null($device->user_id) || $device->user_id === $user->id) {
                    $device->user_id = $user->id;
                    $device->save();
                }
                $activeDevice = $device->load(['warranty', 'repairRequests.tracking.histories', 'repairRequests.remoteSession']);
            }
            // If device does NOT exist in JTS database, activeDevice remains null (unregistered state)
        }

        $token = $user->createToken('auth_token')->plainTextToken;

        return response()->json([
            'message' => 'Login berhasil',
            'token' => $token,
            'user' => $user->load(['devices.warranty', 'devices.repairRequests.tracking.histories']),
            'active_device' => $activeDevice,
            'linked_device' => $activeDevice,
        ]);
    }

    /**
     * Get authenticated user profile.
     */
    public function me(Request $request)
    {
        return response()->json([
            'user' => $request->user()->load(['devices.warranty', 'devices.repairRequests.tracking.histories']),
        ]);
    }

    /**
     * Update user profile.
     */
    public function updateProfile(Request $request)
    {
        $user = $request->user();

        $validated = $request->validate([
            'name' => 'sometimes|required|string|max:255',
            'email' => 'sometimes|required|email|max:255|unique:users,email,' . $user->id,
            'phone' => 'nullable|string|max:30',
            'whatsapp_number' => 'nullable|string|max:30',
            'address' => 'nullable|string',
            'avatar_url' => 'nullable|string',
        ]);

        if (isset($validated['whatsapp_number']) && empty($validated['phone'])) {
            $validated['phone'] = $validated['whatsapp_number'];
        } elseif (isset($validated['phone']) && empty($validated['whatsapp_number'])) {
            $validated['whatsapp_number'] = $validated['phone'];
        }

        $user->update($validated);

        return response()->json([
            'message' => 'Profil berhasil diperbarui',
            'user' => $user->load('devices.warranty'),
        ]);
    }

    /**
     * Upload user profile avatar.
     */
    public function uploadAvatar(Request $request)
    {
        $request->validate([
            'file' => 'required|image|mimes:jpeg,png,jpg,webp|max:5120', // max 5MB
        ]);

        $path = $request->file('file')->store('avatars', 'public');
        $url = \Illuminate\Support\Facades\Storage::disk('public')->url($path);

        $user = $request->user();
        $user->avatar_url = $url;
        $user->save();

        return response()->json([
            'message' => 'Foto profil berhasil diunggah',
            'avatar_url' => $url,
            'user' => $user->load('devices.warranty'),
        ]);
    }

    /**
     * Logout and revoke tokens.
     */
    public function logout(Request $request)
    {
        $request->user()->currentAccessToken()->delete();

        return response()->json([
            'message' => 'Logout berhasil',
        ]);
    }
}
