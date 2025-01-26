using System;
using System.Net;
using System.Data;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Management;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace BotManager;

public partial class DcbmForm : Form
{
    private string currentFile;
    private Process botProcess;
    private string currentDirectory;
    private PerformanceCounter memoryCounter;
    private System.Windows.Forms.Timer updateTimer;

    public DcbmForm()
    {
        InitializeComponent();
        InitializeCustomComponents();
    }

    //***********************************************************************************
    // کد های مربوط به منابع بین سیستم و برنامه
    //***********************************************************************************
    //___________________________________________________________________________________
    private void InitializeCustomComponents()
    {
        currentDirectory = Directory.GetCurrentDirectory();
        memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");

        updateTimer = new System.Windows.Forms.Timer
        {
            Interval = 1000
        };
        updateTimer.Tick += (sender, e) => UpdateDashboardAsync();
        updateTimer.Start();
    }

    private async void UpdateDashboardAsync()
    {
        await Task.Run(() =>
        {
            var cpuUsage = GetCpuUsage();
            var memoryUsage = GetMemoryUsage();

            Invoke((MethodInvoker)delegate
            {
                uptimeLabel.Text = $"{(int)(DateTime.Now - Process.GetCurrentProcess().StartTime).TotalSeconds} ثانیه";
                cpuLabel.Text = $"{cpuUsage}%";
                memoryLabel.Text = $"{memoryUsage}%";
            });
        });
    }

    private string GetCpuUsage()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("select * from Win32_Processor");
            var query = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
            return query?["LoadPercentage"].ToString() ?? "0";
        }
        catch
        {
            return "0";
        }
    }

    private string GetMemoryUsage()
    {
        try
        {
            return memoryCounter.NextValue().ToString("F0");
        }
        catch
        {
            return "0";
        }
    }
    //___________________________________________________________________________________

    //***********************************************************************************
    // کد های مربوط به فعالیت ربات
    //***********************************************************************************
    //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    private void StartBot_Click(object sender, EventArgs e)
    {
        try
        {
            terminalOutputRichTextBox.Text = @"
┏━━━┓━━━━━━━━━━━┏━━━━┓┏━┓┏━┓━━━━┏━━━┓
┃┏━┓┃━━━━━━━━━━━┃┏┓┏┓┃┃┃┗┛┃┃━━━━┃┏━┓┃
┃┃━┃┃┏┓┏┓┏━━┓┏━┓┗┛┃┃┗┛┃┏┓┏┓┃━━┏┓┃┗━┛┃
┃┃━┃┃┃┗┛┃┃┏┓┃┃┏┛━━┃┃━━┃┃┃┃┃┃━━┣┫┃┏┓┏┛
┃┗━┛┃┗┓┏┛┃┃━┫┃┃━━┏┛┗┓━┃┃┃┃┃┃┏┓┃┃┃┃┃┗┓
┗━━━┛━┗┛━┗━━┛┗┛━━┗━━┛━┗┛┗┛┗┛┗┛┗┛┗┛┗━┛
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                                     
                                     ";
            if (botProcess != null && !botProcess.HasExited)
            {
                MessageBox.Show("ربات از قبل در حال اجرا است.", "اطلاع", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Directory.SetCurrentDirectory(currentDirectory);

            botProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "node",
                    Arguments = "index.js",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

                #pragma warning disable CS8604
            botProcess.OutputDataReceived += (s, args) => AppendToTerminal(message: args.Data);
                #pragma warning restore CS8604

                #pragma warning disable CS8604
            botProcess.ErrorDataReceived += (s, args) => AppendToTerminal(message: args.Data);
                #pragma warning restore CS8604

            botProcess.Start();
            botProcess.BeginOutputReadLine();
            botProcess.BeginErrorReadLine();

            AppendToActivity("ربات شروع به کار کرد.");
            AppendToStatus("ربات روشن است");
            statusPictureBox.Image = Properties.Resources.Online1_1;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در شروع ربات: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StopBot_Click(object sender, EventArgs e)
    {
        try
        {
            if (botProcess != null && !botProcess.HasExited)
            {
                AppendToTerminal("در حال توقف ربات...");
                botProcess.Kill();
                botProcess.WaitForExit(5000);

                botProcess.Dispose();
                botProcess = null;

                AppendToTerminal("ربات با موفقیت متوقف شد.");
                AppendToActivity("ربات متوقف شد.");
                AppendToStatus("ربات خاموش است");
                statusPictureBox.Image = Properties.Resources.offline;
            }
            else
            {
                AppendToTerminal("هیچ ربات فعالی برای متوقف کردن وجود ندارد.");
                AppendToActivity("هیچ رباتی برای متوقف کردن وجود ندارد.");
            }
        }
        catch (Exception ex)
        {
            AppendToTerminal($"خطا در توقف ربات: {ex.Message}");
            MessageBox.Show($"خطا در توقف ربات: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void RestartBot_Click(object sender, EventArgs e)
    {
        StopBot_Click(sender, e);
        StartBot_Click(sender, e);
    }

    private void AppendToTerminal(string message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            Invoke((MethodInvoker)delegate
            {
                terminalOutputRichTextBox.AppendText(message + Environment.NewLine);
            });
        }
    }

    private void AppendToActivity(string message)
    {
        Invoke((MethodInvoker)delegate
        {
            activityLabel.Text = $"{message}";
        });
    }

    private void AppendToStatus(string message)
    {
        Invoke((MethodInvoker)delegate
        {
            statusLabel.Text = $"{message}";
        }
        );
    }

    private void ClearTerminal_Click(object sender, EventArgs e)
    {
        terminalOutputRichTextBox.Clear();
    }
    //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    //***********************************************************************************
    // کد های مربوط به مدیریت فایل های ربات
    //***********************************************************************************
    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    private void LoadFiles_Click(object sender, EventArgs e)
    {
        try
        {
            fileListBox.Items.Clear();
            var files = Directory.GetFileSystemEntries(currentDirectory);
            foreach (var file in files)
            {
                fileListBox.Items.Add(Path.GetFileName(file));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری فایل‌ها: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenFile_Click(object sender, EventArgs e)
    {
        if (fileListBox.SelectedItem == null)
        {
            return;
        }

        var selectedFile = fileListBox.SelectedItem.ToString();

        if (string.IsNullOrEmpty(selectedFile))
        {
            return;
        }

        var fullPath = Path.Combine(currentDirectory, selectedFile);

        try
        {
            if (Directory.Exists(fullPath))
            {
                currentDirectory = fullPath;
                LoadFiles_Click(sender, e);
            }
            else if (File.Exists(fullPath))
            {
                currentFile = fullPath;
                fileEditor.Text = File.ReadAllText(fullPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در باز کردن فایل: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private void SaveFile_Click(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(currentFile))
            {
                File.WriteAllText(currentFile, fileEditor.Text);
                MessageBox.Show("فایل ذخیره شد.", "موفقیت", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در ذخیره فایل: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BackButton_Click(object sender, EventArgs e)
    {
        try
        {
            var parentDirectory = Directory.GetParent(currentDirectory)?.FullName;

            if (currentDirectory.Equals(Application.StartupPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("شما در دایرکتوری اصلی هستید و نمی‌توانید به دایرکتوری بالاتر بازگردید.", "هشدار", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!string.IsNullOrEmpty(parentDirectory) &&
                parentDirectory.StartsWith(Application.StartupPath, StringComparison.OrdinalIgnoreCase))
            {
                currentDirectory = parentDirectory;
                LoadFiles_Click(sender, e);
            }
            else
            {
                MessageBox.Show("امکان بازگشت به دایرکتوری بالاتر از دایرکتوری اصلی وجود ندارد.", "هشدار", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بازگشت به دایرکتوری: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void FileListBox_MouseDoubleClick(object sender, MouseEventArgs e)
    {
        if (fileListBox.SelectedItem == null)
        {
            return;
        }

        var selectedFile = fileListBox.SelectedItem.ToString();

        if (string.IsNullOrEmpty(selectedFile))
        {
            return;
        }

        var fullPath = Path.Combine(currentDirectory, selectedFile);

        try
        {
            if (Directory.Exists(fullPath))
            {
                currentDirectory = fullPath;
                LoadFiles_Click(sender, e);
            }
            else if (File.Exists(fullPath))
            {
                currentFile = fullPath;
                fileEditor.Text = File.ReadAllText(fullPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در باز کردن فایل: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearButton_Click(object sender, EventArgs e)
    {
        fileEditor.Clear();
    }

    private void RefreshButton_Click(object sender, EventArgs e)
    {
        try
        {
            fileListBox.Items.Clear();
            var files = Directory.GetFileSystemEntries(currentDirectory);
            foreach (var file in files)
            {
                fileListBox.Items.Add(Path.GetFileName(file));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری فایل‌ها: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

    }
    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    //***********************************************************************************
    // کد های مربوط به فرم
    //***********************************************************************************
    //===================================================================================
    private void DcbmForm_Load(object sender, EventArgs e)
    {
        this.KeyPreview = true;
        DashboardLabel_Load();
        LoadFiles_Click();
        CheckForUpdate();
    }

    private void DashboardLabel_Load()
    {
        dashboardTabLabel.ForeColor = Color.FromArgb(255, 0, 140, 255);
        dashboardTabUnderPanel.BackColor = Color.FromArgb(255, 0, 140, 255);
        dashboardPanel.BringToFront();

        terminalTabUnderPanel.Visible = false;

        fileManagerTabUnderPanel.Visible = false;
    }

    private void LoadFiles_Click()
    {
        try
        {
            fileListBox.Items.Clear();
            var files = Directory.GetFileSystemEntries(currentDirectory);
            foreach (var file in files)
            {
                fileListBox.Items.Add(Path.GetFileName(file));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بارگذاری فایل‌ها: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        }
    //===================================================================================

    //***********************************************************************************
    // کد های مربوط به آپدیت نرم افزار
    //***********************************************************************************
    //-----------------------------------------------------------------------------------
    private const string updateUrl = "https://csharp.mcexe.ir/updates/";
    private const string versionFile = "version.txt";
    private const string updateFile = "BE.zip";
    private const string currentVersion = "1.0.0";

    private void CheckForUpdate()
    {
        try
        {
                #pragma warning disable SYSLIB0014
            using (WebClient client = new())
            {
                string latestVersion = client.DownloadString(updateUrl + "version.txt").Trim();
                string currentVersion = GetCurrentVersion();

                if (currentVersion != latestVersion)
                {
                    var result = MessageBox.Show(text: $"نسخه جدید ({latestVersion}) موجود است. آیا می‌خواهید آپدیت کنید؟", caption: "آپدیت برنامه", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading);
                    if (result == DialogResult.Yes)
                    {
                        DownloadAndUpdate(client, latestVersion);
                        UpdateLocalVersion(latestVersion);
                    }
                }
            }
                #pragma warning restore SYSLIB0014
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در بررسی آپدیت: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }


    private string GetCurrentVersion()
    {
        string versionFilePath = Path.Combine(Application.StartupPath, "version.txt");
        if (File.Exists(versionFilePath))
        {
            return File.ReadAllText(versionFilePath).Trim();
        }
        return "1.0.0";
    }

    private void UpdateLocalVersion(string latestVersion)
    {
        string versionFilePath = Path.Combine(Application.StartupPath, "version.txt");
        File.WriteAllText(versionFilePath, latestVersion);
    }


    private void DownloadAndUpdate(WebClient client, string latestVersion)
    {
        try
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), "update.zip");
            string extractPath = Path.Combine(Path.GetTempPath(), "update_extract");
            string updaterScriptPath = Path.Combine(Path.GetTempPath(), "update_script.bat");

            client.DownloadFile(updateUrl + updateFile, tempFilePath);
            MessageBox.Show("دانلود فایل آپدیت با موفقیت انجام شد.", "اطلاعیه", MessageBoxButtons.OK, MessageBoxIcon.Information);

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(tempFilePath, extractPath);
            File.Delete(tempFilePath);
            MessageBox.Show("استخراج فایل‌ها با موفقیت انجام شد.", "اطلاعیه", MessageBoxButtons.OK, MessageBoxIcon.Information);

            File.WriteAllText(updaterScriptPath, $@"
@echo off
timeout /t 2 >nul
xcopy /s /y ""{extractPath}\*"" ""{Application.StartupPath}\""
start """" ""{Path.Combine(Application.StartupPath, Path.GetFileName(Application.ExecutablePath))}""
del /f /q ""%~f0""
rmdir /s /q ""{extractPath}""
exit
");

            MessageBox.Show("اسکریپت آپدیت با موفقیت ایجاد شد.", "اطلاعیه", MessageBoxButtons.OK, MessageBoxIcon.Information);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{updaterScriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            UpdateLocalVersion(latestVersion);

            Application.Exit();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطا در دانلود یا نصب آپدیت: {ex.Message}", "خطا", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SafeDeleteFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
            File.Delete(filePath);
        }
    }
    //-----------------------------------------------------------------------------------

    //***********************************************************************************
    // کد های مربوط به محیط نرم افزار
    //***********************************************************************************
    //|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||

    private void ExitPictureBox_Click(object sender, EventArgs e)
    {
        Application.Exit();
    }

    private Point mouseLocation;


    private void NavigationBarPictureBox_MouseDown(object sender, MouseEventArgs e)
    {
        mouseLocation = new Point(-e.X, -e.Y);
    }

    private void TopMostCheckBox_CheckedChanged(object sender, EventArgs e)
    {
        if (topMostCheckBox.Checked)
        {
            this.TopMost = true;
        }
        else
        {
            this.TopMost = false;
        }
    }

    private void NavigationBarPictureBox_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            Point mousePose = Control.MousePosition;
            mousePose.Offset(mouseLocation.X, mouseLocation.Y);
            Location = mousePose;
        }
    }

    private void NavigationBarPictureBox_MouseUp(object sender, MouseEventArgs e)
    {
        //Cursor = Cursors.Default;
    }

    private void DashboardLabel_Click(object sender, EventArgs e)
    {
        dashboardTabLabel.ForeColor = Color.FromArgb(255, 0, 140, 255);
        dashboardTabUnderPanel.BackColor = Color.FromArgb(255, 0, 140, 255);
        dashboardPanel.BringToFront();
        dashboardTabUnderPanel.Visible = true;

        terminalTabLabel.ForeColor = Color.White;
        //terminalTabUnderPanel.BackColor = Color.White;
        terminalTabUnderPanel.Visible = false;

        fileManagerTabLabel.ForeColor = Color.White;
        //fileManagerTabUnderPanel.BackColor = Color.White;
        fileManagerTabUnderPanel.Visible = false;

    }

    private void TerminalTabLabel_Click(object sender, EventArgs e)
    {
        terminalTabLabel.ForeColor = Color.FromArgb(255, 0, 140, 255);
        terminalTabUnderPanel.BackColor = Color.FromArgb(255, 0, 140, 255);
        terminalPanel.BringToFront();
        terminalTabUnderPanel.Visible = true;

        dashboardTabLabel.ForeColor = Color.White;
        //dashboardTabUnderPanel.BackColor = Color.White;
        dashboardTabUnderPanel.Visible = false;

        fileManagerTabLabel.ForeColor = Color.White;
        //fileManagerTabUnderPanel.BackColor = Color.White;
        fileManagerTabUnderPanel.Visible = false;
    }

    private void FileManagerTabLabel_Click(object sender, EventArgs e)
    {
        fileManagerTabLabel.ForeColor = Color.FromArgb(255, 0, 140, 255);
        fileManagerTabUnderPanel.BackColor = Color.FromArgb(255, 0, 140, 255);

        fileManagerPanel.Visible = true;
        fileManagerPanel.BringToFront();
        fileManagerTabUnderPanel.Visible = true;
        mainPanel.Visible = true;

        madeByLabel.Visible = true;
        madeByPictureBox.Visible = true;
        myNameLabel.Visible = true;
        introductionLabel.Visible = true;
        introductionPictureBox.Visible = true;

        dashboardTabLabel.ForeColor = Color.White;
        dashboardTabUnderPanel.Visible = false;

        terminalTabLabel.ForeColor = Color.White;
        terminalTabUnderPanel.Visible = false;
    }

    private void DcbmForm_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Control == true && e.Alt == true && e.KeyCode.ToString() == "Q")
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                folderBrowserDialog.Description = "یک دایرکتوری انتخاب کنید:";
                folderBrowserDialog.ShowNewFolderButton = false;

                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    currentDirectory = folderBrowserDialog.SelectedPath;

                    LoadFiles_Click(sender, e);
                }
            }
        }
    }

    private void maximizePictureBox_Click(object sender, EventArgs e)
    {
        if (this.WindowState == FormWindowState.Maximized)
        {
            this.WindowState = FormWindowState.Normal;
        }
        else
        {
            this.WindowState = FormWindowState.Maximized;
        }
    }
    //|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
}
