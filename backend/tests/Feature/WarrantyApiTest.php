<?php

namespace Tests\Feature;

use App\Models\Device;
use App\Models\User;
use App\Models\Warranty;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class WarrantyApiTest extends TestCase
{
    use RefreshDatabase;

    protected function setUp(): void
    {
        parent::setUp();
        $this->seed();
    }

    public function test_can_register_and_auto_bind_device_by_bios_id(): void
    {
        $unassignedDevice = Device::whereNull('user_id')->first();
        $this->assertNotNull($unassignedDevice);

        $response = $this->postJson('/api/auth/register', [
            'name' => 'John Doe',
            'email' => 'johndoe@example.com',
            'password' => 'password123',
            'phone' => '081999888777',
            'address' => 'Jl. Sudirman No. 100',
            'hardware_id' => $unassignedDevice->hardware_id,
        ]);

        $response->assertStatus(201)
            ->assertJsonStructure(['token', 'user', 'linked_device']);

        $device = Device::find($unassignedDevice->id);
        $this->assertNotNull($device->user_id);
    }

    public function test_can_lookup_warranty_by_hardware_id(): void
    {
        $response = $this->getJson('/api/warranties/check/BIOS-UUID-DEMO-001');

        $response->assertStatus(200)
            ->assertJson([
                'status' => 'active',
                'is_active' => true,
            ]);
    }

    public function test_customer_can_calibrate_location(): void
    {
        $customer = User::where('email', 'customer@example.com')->first();
        $device = Device::where('user_id', $customer->id)->first();

        $response = $this->actingAs($customer, 'sanctum')->putJson("/api/devices/{$device->id}/calibrate-location", [
            'location_lat' => -6.200000,
            'location_lng' => 106.816666,
            'location_label' => 'Kantor Baru',
        ]);

        $response->assertStatus(200)
            ->assertJson([
                'device' => [
                    'location_lat' => -6.2,
                    'location_lng' => 106.816666,
                    'location_label' => 'Kantor Baru',
                ]
            ]);
    }

    public function test_physical_damage_creates_onsite_request_with_attached_location(): void
    {
        $customer = User::where('email', 'customer@example.com')->first();
        $device = Device::where('user_id', $customer->id)->first();

        $response = $this->actingAs($customer, 'sanctum')->postJson('/api/repair-requests', [
            'device_id' => $device->id,
            'damage_category' => 'physical',
            'description' => 'Port USB-C longgar dan patah di bagian dalam.',
        ]);

        $response->assertStatus(201)
            ->assertJson([
                'request' => [
                    'damage_category' => 'physical',
                    'type' => 'on_site',
                    'location_confirmed_by_admin' => false,
                ]
            ]);
    }

    public function test_non_physical_damage_creates_remote_request_with_session(): void
    {
        $customer = User::where('email', 'customer@example.com')->first();
        $device = Device::where('user_id', $customer->id)->first();

        $response = $this->actingAs($customer, 'sanctum')->postJson('/api/repair-requests', [
            'device_id' => $device->id,
            'damage_category' => 'non_physical',
            'description' => 'Windows blue screen error CRITICAL_PROCESS_DIED setelah update.',
            'preferred_schedule' => 'Hari Ini - Sesi Siang (13:00 - 15:00 WIB)',
            'rustdesk_session_id' => '123456789',
        ]);

        $response->assertStatus(201)
            ->assertJson([
                'request' => [
                    'type' => 'remote',
                    'preferred_schedule' => 'Hari Ini - Sesi Siang (13:00 - 15:00 WIB)',
                    'remote_session' => [
                        'rustdesk_session_id' => '123456789',
                        'connection_status' => 'waiting_acceptance',
                    ]
                ]
            ]);
    }

    public function test_technician_can_accept_and_schedule_remote_request(): void
    {
        $customer = User::where('email', 'customer@example.com')->first();
        $technician = User::where('role', 'technician')->first();
        $device = Device::where('user_id', $customer->id)->first();

        $createResponse = $this->actingAs($customer, 'sanctum')->postJson('/api/repair-requests', [
            'device_id' => $device->id,
            'damage_category' => 'non_physical',
            'description' => 'Driver WiFi menghilang setelah instalasi software.',
            'preferred_schedule' => 'Hari Ini - Sesi Sore (15:30 - 17:30 WIB)',
            'rustdesk_session_id' => '987654321',
        ]);

        $createResponse->assertStatus(201);
        $requestId = $createResponse->json('request.id');

        // Technician accepts and confirms schedule
        $scheduleResponse = $this->actingAs($technician, 'sanctum')->putJson("/api/repair-requests/{$requestId}/schedule-remote", [
            'scheduled_at' => '2026-09-04 16:00:00',
            'notes' => 'Jadwal remote dikonfirmasi oleh teknisi.',
        ]);

        $scheduleResponse->assertStatus(200)
            ->assertJson([
                'message' => 'Permintaan perbaikan jarak jauh berhasil disetujui dan dijadwalkan',
                'request' => [
                    'id' => $requestId,
                    'status' => 'scheduled',
                    'remote_session' => [
                        'connection_status' => 'scheduled',
                        'technician' => [
                            'id' => $technician->id,
                        ]
                    ]
                ]
            ]);
        $this->assertNotNull($scheduleResponse->json('request.scheduled_at'));
    }

    public function test_admin_can_confirm_onsite_location(): void
    {
        $admin = User::where('role', 'admin')->first();
        $customer = User::where('email', 'customer@example.com')->first();
        $device = Device::where('user_id', $customer->id)->first();

        // Create unconfirmed request
        $request = \App\Models\RepairRequest::create([
            'device_id' => $device->id,
            'user_id' => $customer->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => 'Layar bergaris',
            'location_lat' => -6.21,
            'location_lng' => 106.84,
            'location_confirmed_by_admin' => false,
            'status' => 'pending',
        ]);

        $response = $this->actingAs($admin, 'sanctum')->putJson("/api/repair-requests/{$request->id}/confirm-location", [
            'location_lat' => -6.2105,
            'location_lng' => 106.8410,
        ]);

        $response->assertStatus(200)
            ->assertJson([
                'request' => [
                    'location_confirmed_by_admin' => true,
                    'status' => 'in_progress',
                ]
            ]);
    }

    public function test_technician_can_mark_office_repair_and_update_tracking(): void
    {
        $technician = User::where('role', 'technician')->first();
        $customer = User::where('email', 'customer@example.com')->first();
        $device = Device::where('user_id', $customer->id)->first();

        $request = \App\Models\RepairRequest::create([
            'device_id' => $device->id,
            'user_id' => $customer->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => 'Mati total tersiram air',
            'location_confirmed_by_admin' => true,
            'status' => 'in_progress',
        ]);

        // Step 1: Mark needs office repair
        $response1 = $this->actingAs($technician, 'sanctum')->putJson("/api/repair-requests/{$request->id}/mark-office-repair", [
            'needs_office_repair' => true,
            'notes' => 'Perlu penanganan mesin blower dan sparepart khusus di workshop.',
        ]);

        $response1->assertStatus(200)
            ->assertJson([
                'request' => [
                    'needs_office_repair' => true,
                ]
            ]);

        // Step 2: Update progress to 'di_service_center'
        $response2 = $this->actingAs($technician, 'sanctum')->postJson("/api/repair-requests/{$request->id}/tracking/progress", [
            'status' => 'di_service_center',
            'notes' => 'Unit tiba di lab teknisi.',
        ]);

        $response2->assertStatus(200)
            ->assertJson([
                'tracking' => [
                    'current_status' => 'di_service_center',
                ]
            ]);

        // Step 3: Get tracking timeline
        $response3 = $this->actingAs($customer, 'sanctum')->getJson("/api/repair-requests/{$request->id}/tracking");
        $response3->assertStatus(200)
            ->assertJson([
                'has_tracking' => true,
                'tracking' => [
                    'current_status' => 'di_service_center',
                ]
            ]);
        $this->assertCount(2, $response3->json('tracking.histories'));
    }

    public function test_public_qr_claim_submission(): void
    {
        $response = $this->postJson('/api/qr/claim/qr-demo-lenovo-002', [
            'name' => 'Budi Pelanggan QR',
            'phone' => '082111222333',
            'email' => 'budiqr@example.com',
            'address' => 'Jl. Boulevard Kelapa Gading No. 88, Jakarta Utara',
            'description' => 'Laptop tidak bisa menyala sama sekali setelah mati mendadak.',
            'location_lat' => -6.155,
            'location_lng' => 106.902,
        ]);

        $response->assertStatus(201)
            ->assertJsonStructure(['message', 'request_id', 'device_model']);
    }
}
