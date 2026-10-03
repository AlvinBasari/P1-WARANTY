using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SharedCore.Models
{
    public class UserDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("whatsapp_number")]
        public string? WhatsappNumber { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; } = "customer";

        [JsonPropertyName("devices")]
        public List<DeviceDto>? Devices { get; set; }
    }

    public class DeviceDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("hardware_id")]
        public string HardwareId { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("serial_number")]
        public string SerialNumber { get; set; } = string.Empty;

        [JsonPropertyName("qr_token")]
        public string QrToken { get; set; } = string.Empty;

        [JsonPropertyName("location_lat")]
        public double? LocationLat { get; set; }

        [JsonPropertyName("location_lng")]
        public double? LocationLng { get; set; }

        [JsonPropertyName("location_label")]
        public string? LocationLabel { get; set; }

        [JsonPropertyName("warranty")]
        public WarrantyDto? Warranty { get; set; }

        [JsonPropertyName("repair_requests")]
        public List<RepairRequestDto>? RepairRequests { get; set; }

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }
    }

    public class WarrantyDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("purchase_date")]
        public string? PurchaseDate { get; set; }

        [JsonPropertyName("warranty_start")]
        public string? WarrantyStart { get; set; }

        [JsonPropertyName("warranty_end")]
        public string? WarrantyEnd { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "active";
    }

    public class RepairRequestDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("device_id")]
        public int DeviceId { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("damage_category")]
        public string DamageCategory { get; set; } = "non_physical";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "remote";

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("location_lat")]
        public double? LocationLat { get; set; }

        [JsonPropertyName("location_lng")]
        public double? LocationLng { get; set; }

        [JsonPropertyName("location_confirmed_by_admin")]
        public bool LocationConfirmedByAdmin { get; set; }

        [JsonPropertyName("needs_office_repair")]
        public bool NeedsOfficeRepair { get; set; }

        [JsonPropertyName("preferred_schedule")]
        public string? PreferredSchedule { get; set; }

        [JsonPropertyName("scheduled_at")]
        public string? ScheduledAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "pending";

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("device")]
        public DeviceDto? Device { get; set; }

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }

        [JsonPropertyName("remote_session")]
        public RemoteSessionDto? RemoteSession { get; set; }

        [JsonPropertyName("tracking")]
        public RepairTrackingDto? Tracking { get; set; }
    }

    public class RemoteSessionDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("rustdesk_session_id")]
        public string RustdeskSessionId { get; set; } = string.Empty;

        [JsonPropertyName("scheduled_at")]
        public string? ScheduledAt { get; set; }

        [JsonPropertyName("technician")]
        public UserDto? Technician { get; set; }

        [JsonPropertyName("started_at")]
        public string? StartedAt { get; set; }

        [JsonPropertyName("ended_at")]
        public string? EndedAt { get; set; }

        [JsonPropertyName("connection_status")]
        public string ConnectionStatus { get; set; } = "waiting";

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class RepairTrackingDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("repair_request_id")]
        public int RepairRequestId { get; set; }

        [JsonPropertyName("current_status")]
        public string CurrentStatus { get; set; } = "dijemput";

        [JsonPropertyName("histories")]
        public List<RepairTrackingHistoryDto>? Histories { get; set; }
    }

    public class RepairTrackingHistoryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }
    }

    public class AuthResponseDto
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }

        [JsonPropertyName("active_device")]
        public DeviceDto? ActiveDevice { get; set; }

        [JsonPropertyName("linked_device")]
        public DeviceDto? LinkedDevice { get; set; }
    }

    public class DeviceLookupResponseDto
    {
        [JsonPropertyName("found")]
        public bool Found { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("device")]
        public DeviceDto? Device { get; set; }
    }

    public class WarrantyCheckResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "not_found";

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("device")]
        public DeviceDto? Device { get; set; }

        [JsonPropertyName("warranty")]
        public WarrantyDto? Warranty { get; set; }
    }

    public class TrackingDetailResponseDto
    {
        [JsonPropertyName("has_tracking")]
        public bool HasTracking { get; set; }

        [JsonPropertyName("repair_request")]
        public RepairRequestDto? RepairRequest { get; set; }

        [JsonPropertyName("tracking")]
        public RepairTrackingDto? Tracking { get; set; }
    }

    public class InvoiceDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("invoice_number")]
        public string InvoiceNumber { get; set; } = string.Empty;

        [JsonPropertyName("repair_request_id")]
        public int RepairRequestId { get; set; }

        [JsonPropertyName("device_id")]
        public int DeviceId { get; set; }

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("technician_id")]
        public int? TechnicianId { get; set; }

        [JsonPropertyName("issue_date")]
        public string? IssueDate { get; set; }

        [JsonPropertyName("due_date")]
        public string? DueDate { get; set; }

        [JsonPropertyName("subtotal_amount")]
        public string? SubtotalAmount { get; set; }

        [JsonPropertyName("warranty_discount_amount")]
        public string? WarrantyDiscountAmount { get; set; }

        [JsonPropertyName("tax_amount")]
        public string? TaxAmount { get; set; }

        [JsonPropertyName("total_payable_amount")]
        public string? TotalPayableAmount { get; set; }

        [JsonPropertyName("payment_status")]
        public string PaymentStatus { get; set; } = "paid_by_warranty";

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms_and_conditions")]
        public string? TermsAndConditions { get; set; }

        [JsonPropertyName("items")]
        public List<InvoiceItemDto>? Items { get; set; }

        [JsonPropertyName("device")]
        public DeviceDto? Device { get; set; }

        [JsonPropertyName("user")]
        public UserDto? User { get; set; }

        [JsonPropertyName("technician")]
        public UserDto? Technician { get; set; }

        [JsonPropertyName("repair_request")]
        public RepairRequestDto? RepairRequest { get; set; }
    }

    public class InvoiceItemDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("invoice_id")]
        public int InvoiceId { get; set; }

        [JsonPropertyName("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("item_code")]
        public string? ItemCode { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = "service_fee";

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("unit_price")]
        public string? UnitPrice { get; set; }

        [JsonPropertyName("subtotal")]
        public string? Subtotal { get; set; }

        [JsonPropertyName("is_covered_by_warranty")]
        public bool IsCoveredByWarranty { get; set; } = true;

        [JsonPropertyName("warranty_coverage_amount")]
        public string? WarrantyCoverageAmount { get; set; }

        [JsonPropertyName("customer_payable_amount")]
        public string? CustomerPayableAmount { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class InvoiceResponseDto
    {
        [JsonPropertyName("has_invoice")]
        public bool HasInvoice { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }

        [JsonPropertyName("invoice")]
        public InvoiceDto? Invoice { get; set; }
    }

    public class PaginatedResponseDto<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        [JsonPropertyName("per_page")]
        public int PerPage { get; set; }
    }

    public class InvoiceItemPayloadDto
    {
        [JsonPropertyName("item_name")]
        public string ItemName { get; set; } = string.Empty;

        [JsonPropertyName("item_code")]
        public string? ItemCode { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; } = "service_fee"; // sparepart, service_fee, diagnostic_fee, transport_fee, other

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("unit_price")]
        public decimal UnitPrice { get; set; }

        [JsonPropertyName("is_covered_by_warranty")]
        public bool IsCoveredByWarranty { get; set; } = true;

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    public class CreateInvoicePayloadDto
    {
        [JsonPropertyName("technician_id")]
        public int? TechnicianId { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("terms_and_conditions")]
        public string? TermsAndConditions { get; set; }

        [JsonPropertyName("payment_status")]
        public string? PaymentStatus { get; set; } = "paid_by_warranty";

        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; } = "warranty_claim";

        [JsonPropertyName("items")]
        public List<InvoiceItemPayloadDto> Items { get; set; } = new();
    }

    public class EndRemoteSessionPayloadDto
    {
        [JsonPropertyName("connection_status")]
        public string ConnectionStatus { get; set; } = "completed"; // completed, failed

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }
}
