using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SharedCore.Models;

namespace SharedCore.Services
{
    public class ApiClient
    {
        private HttpClient _httpClient;
        private string? _authToken;

        public string BaseUrl { get; private set; } = "http://127.0.0.1:8000/api";

        public ApiClient(string? baseUrl = null)
        {
            string? configUrl = LoadConfigUrl();

            var effectiveUrl = Environment.GetEnvironmentVariable("WARRANTY_API_URL") 
                ?? configUrl
                ?? baseUrl 
                ?? "http://127.0.0.1:8000/api";

            BaseUrl = effectiveUrl.Trim().TrimEnd('/');
            _httpClient = CreateHttpClient(BaseUrl);
        }

        private static HttpClient CreateHttpClient(string url)
        {
            var formatted = url.EndsWith("/") ? url : url + "/";
            var client = new HttpClient
            {
                BaseAddress = new Uri(formatted),
                Timeout = TimeSpan.FromSeconds(15)
            };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        private static string? LoadConfigUrl()
        {
            try
            {
                var paths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WarrantyCustomerApp", "appsettings.json")
                };

                foreach (var path in paths)
                {
                    if (File.Exists(path))
                    {
                        using var doc = JsonDocument.Parse(File.ReadAllText(path));
                        if (doc.RootElement.TryGetProperty("ServerUrl", out var serverEl))
                        {
                            return serverEl.GetString();
                        }
                        if (doc.RootElement.TryGetProperty("ApiBaseUrl", out var apiEl))
                        {
                            return apiEl.GetString();
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public void UpdateBaseUrl(string newUrl)
        {
            if (string.IsNullOrWhiteSpace(newUrl)) return;

            newUrl = newUrl.Trim();
            if (!newUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !newUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                newUrl = "http://" + newUrl;
            }

            BaseUrl = newUrl.TrimEnd('/');
            _httpClient = CreateHttpClient(BaseUrl);
            if (!string.IsNullOrEmpty(_authToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _authToken);
            }

            // Save to appsettings.json
            SaveConfigUrl(BaseUrl);
        }

        public static void SaveConfigUrl(string url)
        {
            try
            {
                var payload = new
                {
                    ServerUrl = url,
                    AppName = "Warranty Support System - Customer Client",
                    Version = "1.0.0"
                };
                var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

                var paths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WarrantyCustomerApp", "appsettings.json")
                };

                foreach (var path in paths)
                {
                    try
                    {
                        var dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }
                        File.WriteAllText(path, json);
                    }
                    catch { }
                }
            }
            catch { }
        }

        public async Task<(bool Success, string Message, long LatencyMs)> PingAsync(string? testUrl = null)
        {
            var target = testUrl ?? BaseUrl;
            if (string.IsNullOrWhiteSpace(target))
                return (false, "Alamat server kosong", 0);

            target = target.Trim();
            if (!target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                target = "http://" + target;
            }

            var pingEndpoint = target.TrimEnd('/') + "/warranties/check/ping";
            var sw = Stopwatch.StartNew();

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                var response = await client.GetAsync(pingEndpoint);
                sw.Stop();

                // Any HTTP response means server is reachable
                return (true, $"Terhubung ({sw.ElapsedMilliseconds}ms)", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, FormatConnectionException(ex), sw.ElapsedMilliseconds);
            }
        }

        public void SetToken(string token)
        {
            _authToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public void ClearToken()
        {
            _authToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public static string FormatConnectionException(Exception ex)
        {
            if (ex is TaskCanceledException || ex is TimeoutException)
            {
                return "Koneksi ke server PT JTS melebihi batas waktu (timeout). Pastikan koneksi jaringan stabil.";
            }

            var msg = ex.InnerException?.Message ?? ex.Message;
            if (msg.Contains("refused", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("actively refused", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("No connection could be made", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("tidak dapat terhubung", StringComparison.OrdinalIgnoreCase))
            {
                return "Tidak dapat terhubung ke server PT JTS. Pastikan backend aktif dan URL server sesuai.";
            }

            if (msg.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
                msg.Contains("No such host", StringComparison.OrdinalIgnoreCase))
            {
                return "Alamat host server backend tidak ditemukan. Periksa pengaturan URL server.";
            }

            return $"Tidak dapat terhubung ke server backend: {msg}";
        }

        private string ExtractErrorMessage(string resBody, int statusCode)
        {
            if (string.IsNullOrWhiteSpace(resBody))
            {
                return $"Server mengembalikan status error ({statusCode}).";
            }

            string trimmed = resBody.TrimStart();
            if (trimmed.StartsWith("<"))
            {
                return "Server backend mengembalikan respon internal error/HTML. Pastikan ./start-backend.sh versi terbaru sedang berjalan.";
            }

            try
            {
                using var doc = JsonDocument.Parse(resBody);
                if (doc.RootElement.TryGetProperty("message", out var msgEl))
                {
                    string msg = msgEl.GetString() ?? "";
                    if (doc.RootElement.TryGetProperty("errors", out var errsEl) && errsEl.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var prop in errsEl.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                            {
                                return prop.Value[0].GetString() ?? msg;
                            }
                        }
                    }
                    return msg;
                }
            }
            catch
            {
                // Fallback to raw string if not json
            }

            return resBody;
        }

        public async Task<AuthResponseDto> RegisterAsync(
            string name,
            string email,
            string password,
            string? phone,
            string? address,
            string? hardwareId,
            string? model,
            string? serialNumber)
        {
            var payload = new
            {
                name,
                email,
                password,
                phone,
                address,
                hardware_id = hardwareId,
                model,
                serial_number = serialNumber,
                role = "customer"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("auth/register", content);
            }
            catch (Exception ex)
            {
                throw new Exception(FormatConnectionException(ex));
            }

            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            try
            {
                var result = JsonSerializer.Deserialize<AuthResponseDto>(resBody);
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    SetToken(result.Token);
                }
                return result ?? throw new Exception("Respon server kosong.");
            }
            catch (Exception ex) when (ex is not Exception && ex is JsonException)
            {
                throw new Exception("Gagal membaca respon server. Pastikan backend berjalan dengan benar.");
            }
        }

        public async Task<AuthResponseDto> LoginAsync(string email, string password, string? hardwareId = null, string? model = null, string? serialNumber = null)
        {
            var payload = new
            {
                email,
                password,
                hardware_id = hardwareId,
                model,
                serial_number = serialNumber,
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                response = await _httpClient.PostAsync("auth/login", content);
            }
            catch (Exception ex)
            {
                throw new Exception(FormatConnectionException(ex));
            }

            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            try
            {
                var result = JsonSerializer.Deserialize<AuthResponseDto>(resBody);
                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    SetToken(result.Token);
                }
                return result ?? throw new Exception("Respon server kosong.");
            }
            catch (Exception ex) when (ex is not Exception && ex is JsonException)
            {
                throw new Exception("Gagal membaca respon server. Pastikan backend berjalan dengan benar.");
            }
        }

        public async Task<DeviceLookupResponseDto> LookupDeviceAsync(string hardwareId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"devices/lookup/{Uri.EscapeDataString(hardwareId)}");
                var resBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new DeviceLookupResponseDto { Found = false, Message = "Perangkat belum terdaftar" };
                }

                return JsonSerializer.Deserialize<DeviceLookupResponseDto>(resBody) ?? new DeviceLookupResponseDto();
            }
            catch (Exception ex)
            {
                return new DeviceLookupResponseDto { Found = false, Message = ex.Message };
            }
        }

        public async Task<WarrantyCheckResponseDto> CheckWarrantyStatusAsync(string hardwareId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"warranties/check/{Uri.EscapeDataString(hardwareId)}");
                var resBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new WarrantyCheckResponseDto { Status = "not_found", IsActive = false };
                }

                return JsonSerializer.Deserialize<WarrantyCheckResponseDto>(resBody) ?? new WarrantyCheckResponseDto();
            }
            catch
            {
                return new WarrantyCheckResponseDto { Status = "not_found", IsActive = false };
            }
        }

        public async Task<DeviceDto> ClaimDeviceByTokenAsync(string tokenOrSerial, string hardwareId, string? model)
        {
            var payload = new
            {
                token_or_serial = tokenOrSerial,
                hardware_id = hardwareId,
                model = model
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("devices/claim-by-token", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var deviceJson = doc.RootElement.GetProperty("device").GetRawText();
            return JsonSerializer.Deserialize<DeviceDto>(deviceJson)!;
        }

        public async Task<DeviceDto> CalibrateLocationAsync(int deviceId, double lat, double lng, string? label)
        {
            var payload = new
            {
                location_lat = lat,
                location_lng = lng,
                location_label = label
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"devices/{deviceId}/calibrate-location", content);
            var resBody = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(resBody);
            var deviceJson = doc.RootElement.GetProperty("device").GetRawText();
            return JsonSerializer.Deserialize<DeviceDto>(deviceJson)!;
        }

        public async Task<UserDto> UpdateProfileAsync(
            string name,
            string? email = null,
            string? phone = null,
            string? whatsappNumber = null,
            string? address = null,
            string? avatarUrl = null)
        {
            var payload = new Dictionary<string, object?>
            {
                ["name"] = name
            };

            if (!string.IsNullOrWhiteSpace(email)) payload["email"] = email;
            if (!string.IsNullOrWhiteSpace(phone)) payload["phone"] = phone;
            if (!string.IsNullOrWhiteSpace(whatsappNumber)) payload["whatsapp_number"] = whatsappNumber;
            if (!string.IsNullOrWhiteSpace(address)) payload["address"] = address;
            if (!string.IsNullOrWhiteSpace(avatarUrl)) payload["avatar_url"] = avatarUrl;

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync("auth/profile", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var userJson = doc.RootElement.GetProperty("user").GetRawText();
            return JsonSerializer.Deserialize<UserDto>(userJson)!;
        }

        public async Task<string> UploadAvatarAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Berkas foto profil tidak ditemukan.", filePath);
            }

            using var form = new MultipartFormDataContent();
            var fileBytes = await File.ReadAllBytesAsync(filePath);
            var fileContent = new ByteArrayContent(fileBytes);
            
            string ext = Path.GetExtension(filePath).ToLowerInvariant();
            string mimeType = ext switch
            {
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "image/jpeg"
            };

            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            form.Add(fileContent, "file", Path.GetFileName(filePath));

            var response = await _httpClient.PostAsync("auth/avatar", form);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            return doc.RootElement.GetProperty("avatar_url").GetString() ?? "";
        }

        public async Task<RepairRequestDto> CreateRepairRequestAsync(
            int deviceId,
            string damageCategory,
            string description,
            string? rustdeskSessionId = null,
            string? preferredSchedule = null)
        {
            var payload = new
            {
                device_id = deviceId,
                damage_category = damageCategory,
                description,
                rustdesk_session_id = rustdeskSessionId,
                preferred_schedule = preferredSchedule,
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("repair-requests", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var reqJson = doc.RootElement.GetProperty("request").GetRawText();
            return JsonSerializer.Deserialize<RepairRequestDto>(reqJson)!;
        }

        public async Task<RepairRequestDto> GetRepairRequestAsync(int requestId)
        {
            var response = await _httpClient.GetAsync($"repair-requests/{requestId}");
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var reqJson = doc.RootElement.GetProperty("request").GetRawText();
            return JsonSerializer.Deserialize<RepairRequestDto>(reqJson)!;
        }

        public async Task<RepairRequestDto> ScheduleRemoteAsync(int requestId, string scheduledAt, string? notes = null, int? technicianId = null)
        {
            var payload = new
            {
                scheduled_at = scheduledAt,
                notes,
                technician_id = technicianId,
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"repair-requests/{requestId}/schedule-remote", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var reqJson = doc.RootElement.GetProperty("request").GetRawText();
            return JsonSerializer.Deserialize<RepairRequestDto>(reqJson)!;
        }

        public async Task<TrackingDetailResponseDto> GetRepairTrackingAsync(int requestId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"repair-requests/{requestId}/tracking");
                var resBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new TrackingDetailResponseDto { HasTracking = false };
                }

                return JsonSerializer.Deserialize<TrackingDetailResponseDto>(resBody) ?? new TrackingDetailResponseDto();
            }
            catch
            {
                return new TrackingDetailResponseDto { HasTracking = false };
            }
        }

        public async Task<InvoiceResponseDto> GetInvoiceByRepairRequestIdAsync(int requestId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"repair-requests/{requestId}/invoice");
                var resBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new InvoiceResponseDto { HasInvoice = false };
                }

                return JsonSerializer.Deserialize<InvoiceResponseDto>(resBody) ?? new InvoiceResponseDto();
            }
            catch
            {
                return new InvoiceResponseDto { HasInvoice = false };
            }
        }

        public async Task<string> DownloadInvoiceHtmlAsync(int invoiceId)
        {
            var response = await _httpClient.GetAsync($"invoices/{invoiceId}/print");
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            return resBody;
        }

        public async Task<List<RepairRequestDto>> GetRepairRequestsListAsync(
            string? type = null,
            string? damageCategory = null,
            string? status = null,
            bool unconfirmedLocationOnly = false)
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrEmpty(type)) queryParams.Add($"type={Uri.EscapeDataString(type)}");
            if (!string.IsNullOrEmpty(damageCategory)) queryParams.Add($"damage_category={Uri.EscapeDataString(damageCategory)}");
            if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");
            if (unconfirmedLocationOnly) queryParams.Add("unconfirmed_location_only=1");

            string endpoint = "repair-requests" + (queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "");
            var response = await _httpClient.GetAsync(endpoint);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            try
            {
                var paginated = JsonSerializer.Deserialize<PaginatedResponseDto<RepairRequestDto>>(resBody);
                if (paginated?.Data != null)
                {
                    return paginated.Data;
                }
            }
            catch { }

            try
            {
                var list = JsonSerializer.Deserialize<List<RepairRequestDto>>(resBody);
                return list ?? new List<RepairRequestDto>();
            }
            catch
            {
                return new List<RepairRequestDto>();
            }
        }

        public async Task<RepairRequestDto> UpdateRepairStatusAsync(int requestId, string status)
        {
            var payload = new { status };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"repair-requests/{requestId}/status", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var reqJson = doc.RootElement.GetProperty("request").GetRawText();
            return JsonSerializer.Deserialize<RepairRequestDto>(reqJson)!;
        }

        public async Task<RepairRequestDto> MarkOfficeRepairAsync(int requestId, bool needsOfficeRepair, string? notes = null)
        {
            var payload = new
            {
                needs_office_repair = needsOfficeRepair,
                notes
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync($"repair-requests/{requestId}/mark-office-repair", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var reqJson = doc.RootElement.GetProperty("request").GetRawText();
            return JsonSerializer.Deserialize<RepairRequestDto>(reqJson)!;
        }

        public async Task<RepairTrackingDto> UpdateTrackingProgressAsync(int requestId, string status, string? notes = null)
        {
            var payload = new
            {
                status,
                notes
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"repair-requests/{requestId}/tracking/progress", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var trackJson = doc.RootElement.GetProperty("tracking").GetRawText();
            return JsonSerializer.Deserialize<RepairTrackingDto>(trackJson)!;
        }

        public async Task<RemoteSessionDto> StartRemoteSessionAsync(int sessionId)
        {
            var response = await _httpClient.PostAsync($"remote-sessions/{sessionId}/start", new StringContent("{}", Encoding.UTF8, "application/json"));
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var sessionJson = doc.RootElement.GetProperty("session").GetRawText();
            return JsonSerializer.Deserialize<RemoteSessionDto>(sessionJson)!;
        }

        public async Task<RemoteSessionDto> EndRemoteSessionAsync(int sessionId, string connectionStatus, string? notes = null)
        {
            var payload = new
            {
                connection_status = connectionStatus,
                notes
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"remote-sessions/{sessionId}/end", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var sessionJson = doc.RootElement.GetProperty("session").GetRawText();
            return JsonSerializer.Deserialize<RemoteSessionDto>(sessionJson)!;
        }

        public async Task<InvoiceDto> SaveInvoiceAsync(int requestId, CreateInvoicePayloadDto payload)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"repair-requests/{requestId}/invoice", content);
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            using var doc = JsonDocument.Parse(resBody);
            var invJson = doc.RootElement.GetProperty("invoice").GetRawText();
            return JsonSerializer.Deserialize<InvoiceDto>(invJson)!;
        }

        public async Task<List<RemoteSessionDto>> GetRemoteSessionsListAsync()
        {
            var response = await _httpClient.GetAsync("remote-sessions");
            var resBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(ExtractErrorMessage(resBody, (int)response.StatusCode));
            }

            try
            {
                var paginated = JsonSerializer.Deserialize<PaginatedResponseDto<RemoteSessionDto>>(resBody);
                if (paginated?.Data != null)
                {
                    return paginated.Data;
                }
            }
            catch { }

            return new List<RemoteSessionDto>();
        }
    }
}
