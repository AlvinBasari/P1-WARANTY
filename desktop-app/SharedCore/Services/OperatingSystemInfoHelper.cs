using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace SharedCore.Services
{
    /// <summary>
    /// Helper to reliably determine user-friendly Operating System descriptions,
    /// accurately identifying Windows 11 (NT 10.0 build >= 22000) and Linux distributions.
    /// </summary>
    public static class OperatingSystemInfoHelper
    {
        private static string? _cachedFriendlyOs;

        /// <summary>
        /// Returns a human-friendly operating system name with release edition and build.
        /// Example: "Microsoft Windows 11 Home Single Language (25H2, Build 26200)"
        /// </summary>
        public static string GetFriendlyOsDescription()
        {
            if (!string.IsNullOrEmpty(_cachedFriendlyOs))
                return _cachedFriendlyOs;

            try
            {
                if (OperatingSystem.IsWindows())
                {
                    _cachedFriendlyOs = GetWindowsFriendlyDescription();
                }
                else if (OperatingSystem.IsLinux())
                {
                    _cachedFriendlyOs = GetLinuxFriendlyDescription();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    _cachedFriendlyOs = $"macOS {Environment.OSVersion.Version}";
                }
                else
                {
                    _cachedFriendlyOs = RuntimeInformation.OSDescription;
                }
            }
            catch
            {
                _cachedFriendlyOs = RuntimeInformation.OSDescription;
            }

            return _cachedFriendlyOs ?? RuntimeInformation.OSDescription;
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static string GetWindowsFriendlyDescription()
        {
            string productName = string.Empty;
            string displayVersion = string.Empty;
            int buildNumber = Environment.OSVersion.Version.Build;
            int ubr = 0;

            // 1. Fast Registry query from HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key != null)
                {
                    productName = key.GetValue("ProductName") as string ?? string.Empty;
                    displayVersion = key.GetValue("DisplayVersion") as string 
                                     ?? key.GetValue("ReleaseId") as string 
                                     ?? string.Empty;

                    var bStr = key.GetValue("CurrentBuildNumber") as string ?? key.GetValue("CurrentBuild") as string;
                    if (!string.IsNullOrEmpty(bStr) && int.TryParse(bStr, out int b))
                    {
                        buildNumber = b;
                    }

                    if (key.GetValue("UBR") is int ubrVal)
                    {
                        ubr = ubrVal;
                    }
                }
            }
            catch { }

            // 2. Query WMI Win32_OperatingSystem for authentic Caption if available
            string wmiCaption = string.Empty;
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT Caption, BuildNumber FROM Win32_OperatingSystem");
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    string? cap = obj["Caption"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(cap))
                    {
                        wmiCaption = cap;
                    }

                    if (int.TryParse(obj["BuildNumber"]?.ToString(), out int wmiBuild) && wmiBuild > 0)
                    {
                        buildNumber = wmiBuild;
                    }
                    break;
                }
            }
            catch { }

            // Determine base product title: prefer WMI Caption (e.g. "Microsoft Windows 11 Home Single Language")
            // or fallback to Registry ProductName (e.g. "Windows 10 Home Single Language")
            string baseName = !string.IsNullOrWhiteSpace(wmiCaption) ? wmiCaption : productName;
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = buildNumber >= 22000 ? "Microsoft Windows 11" : "Microsoft Windows 10";
            }

            // CRITICAL FIX FOR WINDOWS 11:
            // Microsoft intentionally left ProductName in Registry as "Windows 10 ..." for backward compatibility with legacy installers.
            // Any NT build >= 22000 is officially Windows 11.
            if (buildNumber >= 22000)
            {
                if (baseName.Contains("Windows 10", StringComparison.OrdinalIgnoreCase))
                {
                    baseName = Regex.Replace(baseName, @"Windows\s*10", "Windows 11", RegexOptions.IgnoreCase);
                }
                else if (!baseName.Contains("Windows 11", StringComparison.OrdinalIgnoreCase))
                {
                    baseName = "Microsoft Windows 11";
                }
            }

            // Build version tag: (e.g. "25H2, Build 26200" or "Build 26200")
            string versionTag;
            if (!string.IsNullOrWhiteSpace(displayVersion))
            {
                versionTag = $"{displayVersion}, Build {buildNumber}";
            }
            else
            {
                versionTag = $"Build {buildNumber}";
            }

            return $"{baseName} ({versionTag})";
        }

        private static string GetLinuxFriendlyDescription()
        {
            string prettyName = string.Empty;
            string[] paths = ["/etc/os-release", "/usr/lib/os-release"];

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        foreach (var line in File.ReadAllLines(path))
                        {
                            if (line.StartsWith("PRETTY_NAME=", StringComparison.OrdinalIgnoreCase))
                            {
                                prettyName = line.Substring("PRETTY_NAME=".Length).Trim('\"', '\'');
                                break;
                            }
                        }
                    }
                    catch { }
                }

                if (!string.IsNullOrEmpty(prettyName))
                    break;
            }

            if (!string.IsNullOrEmpty(prettyName))
            {
                return prettyName;
            }

            return RuntimeInformation.OSDescription;
        }

        /// <summary>
        /// Clears cache for testability.
        /// </summary>
        public static void ResetCache()
        {
            _cachedFriendlyOs = null;
        }
    }
}
