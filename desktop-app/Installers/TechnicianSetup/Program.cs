using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace TechnicianSetup
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new InstallerForm());
        }
    }

    public class InstallerForm : Form
    {
        private TextBox txtPath;
        private Button btnBrowse;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkRegisterProtocol;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Button btnInstall;
        private Button btnCancel;
        private CheckBox chkRunNow;
        private string _targetDirectory;

        public InstallerForm()
        {
            Text = "Warranty Support - Technician Client Setup";
            Size = new Size(540, 440);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.FromArgb(248, 250, 252);

            _targetDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WarrantyTechnicianApp"
            );

            // Header Panel
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(15, 23, 42) // Slate Dark
            };

            var lblHeaderTitle = new Label
            {
                Text = "Warranty Support System - Teknisi",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(20, 16),
                AutoSize = true
            };

            var lblHeaderSub = new Label
            {
                Text = "Wisaya Penginstalan Aplikasi Teknisi & Integrasi 1-Klik RustDesk",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(20, 44),
                AutoSize = true
            };

            pnlHeader.Controls.Add(lblHeaderTitle);
            pnlHeader.Controls.Add(lblHeaderSub);
            Controls.Add(pnlHeader);

            // Body Controls
            var lblDir = new Label
            {
                Text = "Lokasi Direktori Instalasi:",
                Location = new Point(24, 105),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            Controls.Add(lblDir);

            txtPath = new TextBox
            {
                Text = _targetDirectory,
                Location = new Point(24, 132),
                Width = 380,
                Font = new Font("Segoe UI", 9.5f)
            };
            Controls.Add(txtPath);

            btnBrowse = new Button
            {
                Text = "Pilih...",
                Location = new Point(415, 130),
                Width = 85,
                Height = 28,
                BackColor = Color.FromArgb(241, 245, 249)
            };
            btnBrowse.Click += (s, e) =>
            {
                using var fbd = new FolderBrowserDialog();
                fbd.SelectedPath = txtPath.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = fbd.SelectedPath;
                }
            };
            Controls.Add(btnBrowse);

            chkDesktop = new CheckBox
            {
                Text = "Buat Shortcut di Desktop",
                Checked = true,
                Location = new Point(26, 175),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(chkDesktop);

            chkStartMenu = new CheckBox
            {
                Text = "Buat Shortcut di Start Menu",
                Checked = true,
                Location = new Point(26, 202),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(chkStartMenu);

            chkRegisterProtocol = new CheckBox
            {
                Text = "Daftarkan integrasi browser (yourapp://) untuk 1-klik remote dari web admin",
                Checked = true,
                Location = new Point(26, 229),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            Controls.Add(chkRegisterProtocol);

            chkRunNow = new CheckBox
            {
                Text = "Jalankan Technician App setelah instalasi selesai",
                Checked = true,
                Location = new Point(26, 256),
                AutoSize = true,
                ForeColor = Color.FromArgb(71, 85, 105),
                Visible = false
            };
            Controls.Add(chkRunNow);

            progressBar = new ProgressBar
            {
                Location = new Point(24, 290),
                Width = 476,
                Height = 20,
                Visible = false
            };
            Controls.Add(progressBar);

            lblStatus = new Label
            {
                Text = "Siap untuk menginstal Technician App dan RustDesk bundle.",
                Location = new Point(24, 318),
                AutoSize = true,
                ForeColor = Color.FromArgb(100, 116, 139)
            };
            Controls.Add(lblStatus);

            // Footer Panel
            var pnlFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 60,
                BackColor = Color.FromArgb(241, 245, 249)
            };

            btnCancel = new Button
            {
                Text = "Batal",
                Location = new Point(415, 15),
                Width = 85,
                Height = 32,
                BackColor = Color.White
            };
            btnCancel.Click += (s, e) => Close();
            pnlFooter.Controls.Add(btnCancel);

            btnInstall = new Button
            {
                Text = "Instal",
                Location = new Point(320, 15),
                Width = 85,
                Height = 32,
                BackColor = Color.FromArgb(16, 185, 129), // Emerald
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += async (s, e) => await StartInstallation();
            pnlFooter.Controls.Add(btnInstall);

            Controls.Add(pnlFooter);
        }

        private async Task StartInstallation()
        {
            _targetDirectory = txtPath.Text.Trim();
            if (string.IsNullOrEmpty(_targetDirectory))
            {
                MessageBox.Show("Silakan pilih direktori tujuan.", "Perhatian", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnInstall.Enabled = false;
            btnBrowse.Enabled = false;
            txtPath.Enabled = false;
            chkDesktop.Enabled = false;
            chkStartMenu.Enabled = false;
            chkRegisterProtocol.Enabled = false;
            btnCancel.Enabled = false;

            progressBar.Visible = true;
            progressBar.Style = ProgressBarStyle.Marquee;
            lblStatus.Text = "Mengekstrak file aplikasi teknisi dan modul remote...";

            bool success = false;
            string error = string.Empty;

            await Task.Run(() =>
            {
                try
                {
                    // 1. Matikan proses lama jika sedang berjalan
                    foreach (var proc in Process.GetProcessesByName("TechnicianApp"))
                    {
                        try { proc.Kill(); proc.WaitForExit(1000); } catch { }
                    }

                    if (!Directory.Exists(_targetDirectory))
                    {
                        Directory.CreateDirectory(_targetDirectory);
                    }

                    // 2. Ekstrak payload.zip dari embedded resource
                    var assembly = Assembly.GetExecutingAssembly();
                    string resourceName = "TechnicianSetup.payload.zip";
                    using var stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        foreach (var name in assembly.GetManifestResourceNames())
                        {
                            if (name.EndsWith("payload.zip", StringComparison.OrdinalIgnoreCase))
                            {
                                using var s = assembly.GetManifestResourceStream(name);
                                if (s != null)
                                {
                                    using var arc = new ZipArchive(s);
                                    arc.ExtractToDirectory(_targetDirectory, overwriteFiles: true);
                                    success = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        using var archive = new ZipArchive(stream);
                        archive.ExtractToDirectory(_targetDirectory, overwriteFiles: true);
                        success = true;
                    }

                    if (success)
                    {
                        string exePath = Path.Combine(_targetDirectory, "TechnicianApp.exe");

                        // 3. Buat Desktop Shortcut
                        if (chkDesktop.Checked)
                        {
                            string desktopPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                                "Warranty Support Technician.lnk"
                            );
                            CreateShortcut(desktopPath, exePath, _targetDirectory);
                        }

                        // 4. Buat Start Menu Shortcut
                        if (chkStartMenu.Checked)
                        {
                            string startMenuPath = Path.Combine(
                                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                                "Programs",
                                "Warranty Support Technician.lnk"
                            );
                            CreateShortcut(startMenuPath, exePath, _targetDirectory);
                        }

                        // 5. Daftarkan Protocol yourapp:// di HKCU Registry
                        if (chkRegisterProtocol.Checked)
                        {
                            RegisterCustomProtocol(exePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    success = false;
                    error = ex.Message;
                }
            });

            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 100;

            if (success)
            {
                lblStatus.Text = "Instalasi Technician App Berhasil!";
                lblStatus.ForeColor = Color.FromArgb(16, 185, 129);
                lblStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);

                chkRunNow.Visible = true;
                btnCancel.Visible = false;

                btnInstall.Text = "Selesai";
                btnInstall.Enabled = true;
                btnInstall.Click -= null;
                btnInstall.Click += (s, e) =>
                {
                    if (chkRunNow.Checked)
                    {
                        string exePath = Path.Combine(_targetDirectory, "TechnicianApp.exe");
                        if (File.Exists(exePath))
                        {
                            Process.Start(new ProcessStartInfo { FileName = exePath, WorkingDirectory = _targetDirectory });
                        }
                    }
                    Close();
                };
            }
            else
            {
                lblStatus.Text = $"Gagal menginstal: {error}";
                lblStatus.ForeColor = Color.Red;
                btnCancel.Enabled = true;
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string workingDir)
        {
            try
            {
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType)!;
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = targetPath;
                    shortcut.WorkingDirectory = workingDir;
                    shortcut.Save();
                }
            }
            catch { }
        }

        private static void RegisterCustomProtocol(string exePath)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(@"Software\Classes\yourapp");
                if (key != null)
                {
                    key.SetValue("", "URL:yourapp Protocol");
                    key.SetValue("URL Protocol", "");

                    using var cmdKey = key.CreateSubKey(@"shell\open\command");
                    if (cmdKey != null)
                    {
                        cmdKey.SetValue("", $"\"{exePath}\" \"%1\"");
                    }
                }
            }
            catch { }
        }
    }
}
