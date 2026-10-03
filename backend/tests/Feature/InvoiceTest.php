<?php

namespace Tests\Feature;

use App\Models\Device;
use App\Models\RepairRequest;
use App\Models\User;
use Illuminate\Foundation\Testing\RefreshDatabase;
use Tests\TestCase;

class InvoiceTest extends TestCase
{
    use RefreshDatabase;

    protected function setUp(): void
    {
        parent::setUp();
        $this->seed();
    }

    public function test_can_create_invoice_with_full_warranty_coverage_resulting_in_zero_cost()
    {
        $admin = User::where('role', 'admin')->first();
        $customer = User::where('email', 'customer@example.com')->first();
        $device = Device::where('user_id', $customer->id)->first();

        $repairRequest = RepairRequest::create([
            'device_id' => $device->id,
            'user_id' => $customer->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => 'Ganti LCD dan Motherboard',
            'status' => 'completed',
        ]);

        $payload = [
            'technician_id' => $admin->id,
            'items' => [
                [
                    'item_name' => 'Penggantian Modul LCD IPS Original',
                    'item_code' => 'LCD-IPS-156-FHD',
                    'category' => 'sparepart',
                    'quantity' => 1,
                    'unit_price' => 1850000,
                    'is_covered_by_warranty' => true,
                ],
                [
                    'item_name' => 'Jasa Servis & Kalibrasi Display',
                    'category' => 'service_fee',
                    'quantity' => 1,
                    'unit_price' => 250000,
                    'is_covered_by_warranty' => true,
                ],
            ],
        ];

        $response = $this->actingAs($admin, 'sanctum')
            ->postJson("/api/repair-requests/{$repairRequest->id}/invoice", $payload);

        $response->assertStatus(200);
        $invoiceData = $response->json('invoice');

        $this->assertEquals(2100000, (float)$invoiceData['subtotal_amount']);
        $this->assertEquals(2100000, (float)$invoiceData['warranty_discount_amount']);
        $this->assertEquals(0, (float)$invoiceData['total_payable_amount']);
        $this->assertEquals('paid_by_warranty', $invoiceData['payment_status']);

        // Customer can fetch the invoice
        $custResponse = $this->actingAs($customer, 'sanctum')
            ->getJson("/api/repair-requests/{$repairRequest->id}/invoice");

        $custResponse->assertStatus(200)
            ->assertJson([
                'has_invoice' => true,
                'invoice' => [
                    'total_payable_amount' => '0.00',
                    'payment_status' => 'paid_by_warranty',
                ]
            ]);
    }

    public function test_can_create_invoice_with_partial_warranty_coverage()
    {
        $admin = User::where('role', 'admin')->first();
        $customer = User::where('email', 'customer@example.com')->first();
        $device = Device::where('user_id', $customer->id)->first();

        $repairRequest = RepairRequest::create([
            'device_id' => $device->id,
            'user_id' => $customer->id,
            'damage_category' => 'physical',
            'type' => 'on_site',
            'description' => 'Ganti Keyboard (Garansi) + Upgrade RAM 16GB (Biaya Klien)',
            'status' => 'completed',
        ]);

        $payload = [
            'technician_id' => $admin->id,
            'items' => [
                [
                    'item_name' => 'Penggantian Keyboard Original',
                    'item_code' => 'KB-LEN-T14-ORIG',
                    'category' => 'sparepart',
                    'quantity' => 1,
                    'unit_price' => 750000,
                    'is_covered_by_warranty' => true,
                ],
                [
                    'item_name' => 'Upgrade RAM Kingston DDR4 16GB (Permintaan Klien)',
                    'item_code' => 'RAM-DDR4-16GB-3200',
                    'category' => 'sparepart',
                    'quantity' => 1,
                    'unit_price' => 600000,
                    'is_covered_by_warranty' => false,
                ],
            ],
        ];

        $response = $this->actingAs($admin, 'sanctum')
            ->postJson("/api/repair-requests/{$repairRequest->id}/invoice", $payload);

        $response->assertStatus(200);
        $invoiceData = $response->json('invoice');

        $this->assertEquals(1350000, (float)$invoiceData['subtotal_amount']);
        $this->assertEquals(750000, (float)$invoiceData['warranty_discount_amount']);
        $this->assertEquals(600000, (float)$invoiceData['total_payable_amount']);
        $this->assertEquals('unpaid', $invoiceData['payment_status']);
    }
}
