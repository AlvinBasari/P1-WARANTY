using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SharedCore.Services
{
    public class DeviceIdentifierService
    {
        private static string? _cachedHardwareId;
        private static string? _cachedModelName;

        /// <summary>
        /// Automatically extracts the unique hardware BIOS UUID / Serial Number (FR-01).
        /// </summary>
        public string GetHardwareId()
        {
            if (!string.IsNullOrEmpty(_cachedHardwareId))
                return _cachedHardwareId;

            string id = string.Empty;

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    id = GetWindowsBiosId();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    id = GetLinuxBiosId();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    id = GetMacHardwareId();
                }
            }
            catch
            {
                // Fallback to MAC/Machine-based ID
            }

            if (string.IsNullOrWhiteSpace(id) || id.Contains("To be filled", StringComparison.OrdinalIgnoreCase))
            {
                id = GetFallbackUniqueId();
            }

            _cachedHardwareId = id.Trim();
            return _cachedHardwareId;
        }

        /// <summary>
        /// Reads the manufacturer and model name of the physical computer.
        /// </summary>
        public string GetDeviceModel()
        {
            if (!string.IsNullOrEmpty(_cachedModelName))
                return _cachedModelName;

            string model = string.Empty;

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    model = RunCommand("wmic", "computersystem get model");
                    model = CleanWmicOutput(model);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    if (File.Exists("/sys/class/dmi/id/product_name"))
                    {
                        model = File.ReadAllText("/sys/class/dmi/id/product_name").Trim();
                    }
                }
            }
            catch
            {
                // Fallback
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                model = $"{Environment.MachineName} ({RuntimeInformation.OSDescription})";
            }

            _cachedModelName = model;
            return _cachedModelName;
        }

        /// <summary>
        /// Reads physical serial number of the machine.
        /// </summary>
        public string GetSerialNumber()
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/sys/class/dmi/id/product_serial"))
                {
                    string serial = File.ReadAllText("/sys/class/dmi/id/product_serial").Trim();
                    if (!string.IsNullOrEmpty(serial) && !serial.Contains("None", StringComparison.OrdinalIgnoreCase) && !serial.Contains("Default", StringComparison.OrdinalIgnoreCase))
                        return serial;
                }
            }
            catch { }

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/etc/machine-id"))
                {
                    string mid = File.ReadAllText("/etc/machine-id").Trim();
                    if (!string.IsNullOrEmpty(mid))
                        return "SN-" + mid.Substring(0, Math.Min(12, mid.Length)).ToUpperInvariant();
                }
            }
            catch { }

            return "SN-JTS-" + Environment.MachineName.ToUpperInvariant();
        }

        private string GetWindowsBiosId()
        {
            // Try BIOS serial number first
            string serial = RunCommand("wmic", "bios get serialnumber");
            serial = CleanWmicOutput(serial);

            if (!string.IsNullOrEmpty(serial) && !serial.Equals("0", StringComparison.OrdinalIgnoreCase))
                return $"BIOS-SN-{serial}";

            // Try UUID
            string uuid = RunCommand("wmic", "csproduct get uuid");
            uuid = CleanWmicOutput(uuid);

            if (!string.IsNullOrEmpty(uuid))
                return $"BIOS-UUID-{uuid}";

            return string.Empty;
        }

        private string GetLinuxBiosId()
        {
            // 1. Try DMI product_uuid (requires root or readable in modern distros)
            if (File.Exists("/sys/class/dmi/id/product_uuid"))
            {
                try
                {
                    string uuid = File.ReadAllText("/sys/class/dmi/id/product_uuid").Trim();
                    if (!string.IsNullOrEmpty(uuid))
                        return $"BIOS-UUID-{uuid}";
                }
                catch { }
            }

            // 2. Try DMI product_serial
            if (File.Exists("/sys/class/dmi/id/product_serial"))
            {
                try
                {
                    string serial = File.ReadAllText("/sys/class/dmi/id/product_serial").Trim();
                    if (!string.IsNullOrEmpty(serial))
                        return $"BIOS-SN-{serial}";
                }
                catch { }
            }

            // 3. Fallback to /etc/machine-id
            if (File.Exists("/etc/machine-id"))
            {
                try
                {
                    string machineId = File.ReadAllText("/etc/machine-id").Trim();
                    if (!string.IsNullOrEmpty(machineId))
                        return $"MACHINE-{machineId}";
                }
                catch { }
            }

            return string.Empty;
        }

        private string GetMacHardwareId()
        {
            string output = RunCommand("ioreg", "-rd1 -c IOPlatformExpertDevice");
            foreach (var line in output.Split('\n'))
            {
                if (line.Contains("IOPlatformUUID"))
                {
                    var parts = line.Split('=');
                    if (parts.Length > 1)
                        return $"MAC-UUID-{parts[1].Replace("\"", "").Trim()}";
                }
            }
            return string.Empty;
        }

        private string GetFallbackUniqueId()
        {
            // Deterministic hash based on Machine Name, User, and first Network Interface MAC
            StringBuilder sb = new StringBuilder();
            sb.Append(Environment.MachineName);
            sb.Append(Environment.UserName);
            sb.Append(Environment.ProcessorCount);

            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up && !nic.Description.Contains("Virtual"))
                {
                    sb.Append(nic.GetPhysicalAddress().ToString());
                    break;
                }
            }

            using var sha256 = SHA256.Create();
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return "HW-" + Convert.ToHexString(hash)[..16];
        }

        private string CleanWmicOutput(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return string.Empty;
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.Length > 1 ? lines[1].Trim() : (lines.Length > 0 ? lines[0].Trim() : string.Empty);
        }

        private string RunCommand(string binary, string args)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = binary,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(2000);
                return output;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
