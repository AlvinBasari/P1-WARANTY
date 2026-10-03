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
        Schema::create('repair_requests', function (Blueprint $table) {
            $table->id();
            $table->foreignId('device_id')->constrained('devices')->cascadeOnDelete();
            $table->foreignId('user_id')->nullable()->constrained('users')->nullOnDelete();
            $table->string('damage_category')->default('non_physical'); // physical, non_physical
            $table->string('type')->default('remote'); // remote, on_site
            $table->text('description');
            $table->json('attachments')->nullable();
            $table->double('location_lat', 10, 7)->nullable();
            $table->double('location_lng', 10, 7)->nullable();
            $table->boolean('location_confirmed_by_admin')->default(false);
            $table->boolean('needs_office_repair')->default(false);
            $table->string('preferred_schedule')->nullable();
            $table->timestamp('scheduled_at')->nullable();
            $table->string('status')->default('pending'); // pending, scheduled, in_progress, completed, rejected, cancelled
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('repair_requests');
    }
};
