<?php

use Illuminate\Database\Migrations\Migration;
use Illuminate\Database\Schema\Blueprint;
use Illuminate\Support\Facades\Schema;

return new class extends Migration
{
    /**
     * Run the migrations.
     */
    public function up(): void
    {
        Schema::create('invoices', function (Blueprint $table) {
            $table->id();
            $table->string('invoice_number')->unique(); // e.g. INV-202609-0001
            $table->foreignId('repair_request_id')->constrained('repair_requests')->cascadeOnDelete();
            $table->foreignId('device_id')->constrained('devices')->cascadeOnDelete();
            $table->foreignId('user_id')->nullable()->constrained('users')->nullOnDelete();
            $table->foreignId('technician_id')->nullable()->constrained('users')->nullOnDelete();
            $table->timestamp('issue_date')->useCurrent();
            $table->timestamp('due_date')->nullable();
            
            $table->decimal('subtotal_amount', 14, 2)->default(0);
            $table->decimal('warranty_discount_amount', 14, 2)->default(0);
            $table->decimal('tax_amount', 14, 2)->default(0);
            $table->decimal('total_payable_amount', 14, 2)->default(0);
            
            $table->string('payment_status')->default('paid_by_warranty'); // paid_by_warranty, paid, unpaid, partially_paid, cancelled
            $table->string('payment_method')->nullable()->default('Jaminan Garansi Resmi PT JTS');
            $table->text('notes')->nullable();
            $table->text('terms_and_conditions')->nullable();
            $table->timestamps();
        });

        Schema::create('invoice_items', function (Blueprint $table) {
            $table->id();
            $table->foreignId('invoice_id')->constrained('invoices')->cascadeOnDelete();
            $table->string('item_name');
            $table->string('item_code')->nullable();
            $table->string('category')->default('service_fee'); // sparepart, service_fee, diagnostic_fee, transport_fee
            $table->integer('quantity')->default(1);
            $table->decimal('unit_price', 14, 2)->default(0);
            $table->decimal('subtotal', 14, 2)->default(0);
            
            $table->boolean('is_covered_by_warranty')->default(true);
            $table->decimal('warranty_coverage_amount', 14, 2)->default(0);
            $table->decimal('customer_payable_amount', 14, 2)->default(0);
            
            $table->string('notes')->nullable();
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('invoice_items');
        Schema::dropIfExists('invoices');
    }
};
