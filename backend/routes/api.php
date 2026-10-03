<?php

use App\Http\Controllers\Api\AuthController;
use App\Http\Controllers\Api\DeviceController;
use App\Http\Controllers\Api\WarrantyController;
use App\Http\Controllers\Api\RepairRequestController;
use App\Http\Controllers\Api\RepairTrackingController;
use App\Http\Controllers\Api\RemoteSessionController;
use App\Http\Controllers\Api\QrClaimController;
use Illuminate\Support\Facades\Route;

/*
|--------------------------------------------------------------------------
| Public Routes
|--------------------------------------------------------------------------
*/
Route::post('/auth/register', [AuthController::class, 'register']);
Route::post('/auth/login', [AuthController::class, 'login']);

// Hardware BIOS Lookup (Used by desktop on init)
Route::get('/devices/lookup/{hardwareId}', [DeviceController::class, 'lookupByHardwareId']);
Route::get('/warranties/check/{hardwareId}', [WarrantyController::class, 'checkStatus']);

// Public QR Code Endpoints (Accessed by customer without login via QR scan)
Route::get('/qr/lookup/{token}', [QrClaimController::class, 'lookup']);
Route::post('/qr/claim/{token}', [QrClaimController::class, 'submitClaim']);

// Public upload (or authenticated)
Route::post('/attachments/upload', [RepairRequestController::class, 'uploadAttachment']);

/*
|--------------------------------------------------------------------------
| Protected Routes (Sanctum Auth)
|--------------------------------------------------------------------------
*/
Route::middleware('auth:sanctum')->group(function () {
    // User Profile
    Route::get('/auth/me', [AuthController::class, 'me']);
    Route::put('/auth/profile', [AuthController::class, 'updateProfile']);
    Route::post('/auth/avatar', [AuthController::class, 'uploadAvatar']);
    Route::post('/auth/logout', [AuthController::class, 'logout']);

    // Devices & Location Calibration
    Route::get('/devices', [DeviceController::class, 'index']);
    Route::get('/devices/{id}', [DeviceController::class, 'show']);
    Route::post('/devices', [DeviceController::class, 'store']);
    Route::post('/devices/claim-by-token', [DeviceController::class, 'claimByToken']);
    Route::put('/devices/{id}/calibrate-location', [DeviceController::class, 'calibrateLocation']);

    // Warranties
    Route::get('/warranties', [WarrantyController::class, 'index']);
    Route::put('/warranties/{id}', [WarrantyController::class, 'update']);

    // Repair Requests
    Route::get('/repair-requests', [RepairRequestController::class, 'index']);
    Route::get('/repair-requests/{id}', [RepairRequestController::class, 'show']);
    Route::post('/repair-requests', [RepairRequestController::class, 'store']);
    Route::put('/repair-requests/{id}/status', [RepairRequestController::class, 'updateStatus']);
    Route::put('/repair-requests/{id}/confirm-location', [RepairRequestController::class, 'confirmLocation']);
    Route::put('/repair-requests/{id}/schedule-remote', [RepairRequestController::class, 'scheduleRemote']);
    Route::put('/repair-requests/{id}/mark-office-repair', [RepairRequestController::class, 'markOfficeRepair']);

    // Repair Tracking (Office/Service Center Stepper)
    Route::get('/repair-requests/{requestId}/tracking', [RepairTrackingController::class, 'show']);
    Route::post('/repair-requests/{requestId}/tracking/progress', [RepairTrackingController::class, 'updateProgress']);

    // Remote Sessions (RustDesk Audit Logs)
    Route::get('/remote-sessions', [RemoteSessionController::class, 'index']);
    Route::post('/remote-sessions/{id}/start', [RemoteSessionController::class, 'startSession']);
    Route::post('/remote-sessions/{id}/end', [RemoteSessionController::class, 'endSession']);

    // Invoices & Warranty Deductions
    Route::get('/invoices', [\App\Http\Controllers\Api\InvoiceController::class, 'index']);
    Route::get('/invoices/{id}', [\App\Http\Controllers\Api\InvoiceController::class, 'show']);
    Route::get('/invoices/{id}/print', [\App\Http\Controllers\Api\InvoiceController::class, 'downloadInvoiceHtml']);
    Route::get('/repair-requests/{requestId}/invoice', [\App\Http\Controllers\Api\InvoiceController::class, 'getByRepairRequest']);
    Route::post('/repair-requests/{requestId}/invoice', [\App\Http\Controllers\Api\InvoiceController::class, 'storeOrUpdate']);
});
