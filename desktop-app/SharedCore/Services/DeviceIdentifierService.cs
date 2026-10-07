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
                if (OperatingSystem.IsWindows())
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
                if (OperatingSystem.IsWindows())
                {
                    model = GetWindowsModelInternal();
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
                model = $"{Environment.MachineName} ({OperatingSystemInfoHelper.GetFriendlyOsDescription()})";
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
                if (OperatingSystem.IsWindows())
                {
                    string winSerial = GetWindowsSerialNumberInternal();
                    if (!string.IsNullOrEmpty(winSerial))
                        return winSerial;
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && File.Exists("/sys/class/dmi/id/product_serial"))
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

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private string GetWindowsBiosId()
        {
            try
            {
                return GetWindowsBiosIdInternal();
            }
            catch
            {
                return string.Empty;
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private string GetWindowsModelInternal()
        {
            // 1. Try Windows Registry (instant, no WMI service dependency)
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
                if (key != null)
                {
                    string mfg = (key.GetValue("SystemManufacturer") as string ?? "").Trim();
                    string family = (key.GetValue("SystemFamily") as string ?? "").Trim();
                    string prod = (key.GetValue("SystemProductName") as string ?? "").Trim();
                    string ver = (key.GetValue("SystemVersion") as string ?? "").Trim();

                    bool isValid(string s) => !string.IsNullOrEmpty(s) &&
                                              !s.Contains("Default", StringComparison.OrdinalIgnoreCase) &&
                                              !s.Contains("To be filled", StringComparison.OrdinalIgnoreCase) &&
                                              !s.Contains("System Product", StringComparison.OrdinalIgnoreCase) &&
                                              !s.Contains("None", StringComparison.OrdinalIgnoreCase);

                    string resolvedModel = "";
                    if (isValid(family))
                        resolvedModel = family;
                    else if (isValid(ver))
                        resolvedModel = ver;
                    else if (isValid(prod))
                        resolvedModel = prod;

                    if (!string.IsNullOrEmpty(resolvedModel))
                    {
                        if (isValid(mfg) && !resolvedModel.StartsWith(mfg, StringComparison.OrdinalIgnoreCase))
                        {
                            if (isValid(prod) && !resolvedModel.Contains(prod, StringComparison.OrdinalIgnoreCase))
                                return $"{mfg} {resolvedModel} ({prod})";
                            return $"{mfg} {resolvedModel}";
                        }
                        return resolvedModel;
                    }
                }
            }
            catch { }

            // 2. Try WMI ManagementObjectSearcher
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT Manufacturer, Model, SystemFamily FROM Win32_ComputerSystem");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string mfg = (obj["Manufacturer"]?.ToString() ?? "").Trim();
                    string model = (obj["Model"]?.ToString() ?? "").Trim();
                    string family = (obj["SystemFamily"]?.ToString() ?? "").Trim();

                    string best = !string.IsNullOrEmpty(family) && !family.Contains("Default", StringComparison.OrdinalIgnoreCase)
                        ? (!string.IsNullOrEmpty(model) && !family.Contains(model, StringComparison.OrdinalIgnoreCase) ? $"{family} ({model})" : family)
                        : model;

                    if (!string.IsNullOrEmpty(best))
                    {
                        if (!string.IsNullOrEmpty(mfg) && !best.StartsWith(mfg, StringComparison.OrdinalIgnoreCase))
                            return $"{mfg} {best}";
                        return best;
                    }
                }
            }
            catch { }

            // 3. Fallback to PowerShell CIM
            try
            {
                string output = RunCommand("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_ComputerSystem).Model\"");
                output = output?.Trim() ?? "";
                if (!string.IsNullOrEmpty(output) && !output.Contains("Error", StringComparison.OrdinalIgnoreCase))
                    return output;
            }
            catch { }

            // 4. Try legacy wmic
            try
            {
                string wmicOut = RunCommand("wmic", "computersystem get model");
                string cleaned = CleanWmicOutput(wmicOut);
                if (!string.IsNullOrEmpty(cleaned)) return cleaned;
            }
            catch { }

            return string.Empty;
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private string GetWindowsSerialNumberInternal()
        {
            // 1. Try WMI Win32_Bios
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_Bios");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string serial = obj["SerialNumber"]?.ToString()?.Trim() ?? "";
                    if (IsValidSerial(serial))
                        return serial;
                }
            }
            catch { }

            // 2. Try WMI Win32_BaseBoard
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string serial = obj["SerialNumber"]?.ToString()?.Trim() ?? "";
                    if (IsValidSerial(serial))
                        return serial;
                }
            }
            catch { }

            // 3. Try PowerShell CIM
            try
            {
                string serial = RunCommand("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_Bios).SerialNumber\"");
                serial = serial?.Trim() ?? "";
                if (IsValidSerial(serial))
                    return serial;
            }
            catch { }

            // 4. Try legacy wmic
            try
            {
                string serial = RunCommand("wmic", "bios get serialnumber");
                serial = CleanWmicOutput(serial);
                if (IsValidSerial(serial))
                    return serial;
            }
            catch { }

            return "SN-JTS-" + Environment.MachineName.ToUpperInvariant();
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private string GetWindowsBiosIdInternal()
        {
            // 1. Try WMI Win32_Bios SerialNumber & Win32_ComputerSystemProduct UUID
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_Bios");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string serial = obj["SerialNumber"]?.ToString()?.Trim() ?? "";
                    if (IsValidSerial(serial))
                        return $"BIOS-SN-{serial}";
                }
            }
            catch { }

            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string uuid = obj["UUID"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(uuid) && !uuid.Contains("00000000") && !uuid.Contains("FFFFFFFF", StringComparison.OrdinalIgnoreCase))
                        return $"BIOS-UUID-{uuid}";
                }
            }
            catch { }

            // 2. Try PowerShell CIM
            try
            {
                string serial = RunCommand("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_Bios).SerialNumber\"");
                serial = serial?.Trim() ?? "";
                if (IsValidSerial(serial))
                    return $"BIOS-SN-{serial}";

                string uuid = RunCommand("powershell", "-NoProfile -Command \"(Get-CimInstance Win32_ComputerSystemProduct).UUID\"");
                uuid = uuid?.Trim() ?? "";
                if (!string.IsNullOrEmpty(uuid) && !uuid.Contains("00000000"))
                    return $"BIOS-UUID-{uuid}";
            }
            catch { }

            // 3. Try legacy wmic (older Windows)
            try
            {
                string serial = RunCommand("wmic", "bios get serialnumber");
                serial = CleanWmicOutput(serial);
                if (IsValidSerial(serial))
                    return $"BIOS-SN-{serial}";

                string uuid = RunCommand("wmic", "csproduct get uuid");
                uuid = CleanWmicOutput(uuid);
                if (!string.IsNullOrEmpty(uuid))
                    return $"BIOS-UUID-{uuid}";
            }
            catch { }

            return string.Empty;
        }

        private static bool IsValidSerial(string? serial)
        {
            if (string.IsNullOrWhiteSpace(serial)) return false;
            if (serial.Equals("0", StringComparison.OrdinalIgnoreCase)) return false;
            if (serial.Equals("None", StringComparison.OrdinalIgnoreCase)) return false;
            if (serial.Contains("Default", StringComparison.OrdinalIgnoreCase)) return false;
            if (serial.Contains("To be filled", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
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
