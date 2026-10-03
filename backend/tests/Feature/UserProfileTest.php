<?php

namespace Tests\Feature;

use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Illuminate\Http\UploadedFile;
use Illuminate\Support\Facades\Storage;
use Tests\TestCase;

class UserProfileTest extends TestCase
{
    use RefreshDatabase;

    protected function setUp(): void
    {
        parent::setUp();
        $this->seed();
    }

    public function test_customer_can_update_profile_data_and_whatsapp_number()
    {
        $customer = User::where('email', 'customer@example.com')->first();

        $response = $this->actingAs($customer, 'sanctum')->putJson('/api/auth/profile', [
            'name' => 'Budi Santoso Updated',
            'phone' => '081234567890',
            'whatsapp_number' => '081234567890',
            'address' => 'Jl. Kebon Jeruk No. 25, Jakarta Barat',
        ]);

        $response->assertStatus(200)
            ->assertJson([
                'message' => 'Profil berhasil diperbarui',
                'user' => [
                    'name' => 'Budi Santoso Updated',
                    'whatsapp_number' => '081234567890',
                    'address' => 'Jl. Kebon Jeruk No. 25, Jakarta Barat',
                ]
            ]);

        $this->assertDatabaseHas('users', [
            'id' => $customer->id,
            'name' => 'Budi Santoso Updated',
            'whatsapp_number' => '081234567890',
        ]);
    }

    public function test_customer_can_upload_avatar_image()
    {
        Storage::fake('public');
        $customer = User::where('email', 'customer@example.com')->first();

        $file = UploadedFile::fake()->create('avatar.jpg', 50, 'image/jpeg');

        $response = $this->actingAs($customer, 'sanctum')->postJson('/api/auth/avatar', [
            'file' => $file,
        ]);

        $response->assertStatus(200)
            ->assertJsonStructure([
                'message',
                'avatar_url',
                'user' => ['id', 'avatar_url'],
            ]);

        $customer->refresh();
        $this->assertNotNull($customer->avatar_url);
    }
}
