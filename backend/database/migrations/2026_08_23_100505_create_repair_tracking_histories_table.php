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
        Schema::create('repair_tracking_histories', function (Blueprint $table) {
            $table->id();
            $table->foreignId('repair_tracking_id')->constrained('repair_trackings')->cascadeOnDelete();
            $table->string('status'); // dijemput, di_service_center, sedang_diperbaiki, selesai, dikembalikan
            $table->foreignId('changed_by')->nullable()->constrained('users')->nullOnDelete();
            $table->text('notes')->nullable();
            $table->timestamps();
        });
    }

    /**
     * Reverse the migrations.
     */
    public function down(): void
    {
        Schema::dropIfExists('repair_tracking_histories');
    }
};
