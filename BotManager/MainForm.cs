using Aeat;
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

public partial class MainForm : Form
{
    private string currentFile;
    private Process botProcess;
    private string currentDirectory;
    private FileSystemWatcher fileWatcher;
    private PerformanceCounter memoryCounter;
    private MrContextMenuStrip fileContextMenu;
    private System.Windows.Forms.Timer updateTimer;
    private Stack<string> backStack = new Stack<string>();
    private Stack<string> forwardStack = new Stack<string>();

    public MainForm()
    {
        InitializeComponent();
        InitializeFileContextMenu();
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

    private DateTime? botStartTime = null;

    private async void UpdateDashboardAsync()
    {
        await Task.Run(() =>
        {
            var cpuUsage = GetCpuUsage();
            var memoryUsage = GetMemoryUsage();

            Invoke((MethodInvoker)delegate
            {
                if (botStartTime.HasValue)
                {
                    var uptime = (int)(DateTime.Now - botStartTime.Value).TotalSeconds;
                    uptimeLabel.Text = $"{uptime} ثانیه";
                }
                else
                {
                    uptimeLabel.Text = "خاموش";
                }

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
                MessageBox.Show(text: "ربات از قبل در حال اجرا است.", caption: "اطلاع", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
                return;
            }

            Directory.SetCurrentDirectory(currentDirectory);
            botStartTime = DateTime.Now;

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

            botProcess.OutputDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    AppendToTerminal(message: args.Data);

                    if (statusPictureBox.Image != Properties.Resources.Online1_1)
                    {
                        this.Invoke(new Action(() =>
                        {
                            statusPictureBox.Image = Properties.Resources.Online1_1;
                            AppendToActivity("ربات شروع به کار کرد.");
                            AppendToStatus("ربات روشن است");
                        }));
                    }
                }
            };

            botProcess.ErrorDataReceived += (s, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    AppendToTerminal(message: args.Data);
                }
            };

            botProcess.Start();
            botProcess.BeginOutputReadLine();
            botProcess.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در شروع ربات: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
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
                botStartTime = null;

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
            MessageBox.Show(text: $"خطا در توقف ربات: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
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
            MessageBox.Show(text: $"خطا در بارگذاری فایل‌ها: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
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
            MessageBox.Show(text: $"خطا در باز کردن فایل: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void FileListBox_DrawItem(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        string itemText = fileListBox.Items[e.Index].ToString();
        string fullPath = Path.Combine(currentDirectory, itemText);

        bool isDirectory = Directory.Exists(fullPath);
        Color backColor = isDirectory ? Color.FromArgb(0, 85, 255) : Color.Transparent;

        e.Graphics.FillRectangle(new SolidBrush(backColor), e.Bounds);
        e.Graphics.DrawString(itemText, e.Font, Brushes.White, e.Bounds, StringFormat.GenericDefault);

        e.DrawFocusRectangle();
    }

    private void MyNameLabel_Click(object sender, EventArgs e)
    {
        try
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "https://mr-maeazadi.mcexe.ir/",
                UseShellExecute = true,
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در باز کردن لینک: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void FileListBox_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.XButton1)
        {
            BackButton_Click(sender, e);
        }

        if (e.Button == MouseButtons.Right)
        {
            int index = fileListBox.IndexFromPoint(e.Location);

            if (index != ListBox.NoMatches)
            {
                fileListBox.SelectedIndex = index;
            }
            else
            {
                fileListBox.ClearSelected();
            }

            fileContextMenu.Show(fileListBox, e.Location);
        }
    }

    private void InitializeFileContextMenu()
    {
        fileContextMenu = new MrContextMenuStrip();

        fileContextMenu.Items.Add("کپی", null, (s, e) => CopyFileOrFolder());
        fileContextMenu.Items.Add("برش", null, (s, e) => CutFileOrFolder());
        fileContextMenu.Items.Add("تازه سازی", null, (s, e) => UpdateFileListBox(currentDirectory));
        fileContextMenu.Items.Add("تغییر نام", null, (s, e) => Rename());
        fileContextMenu.Items.Add("فایل جدید", null, (s, e) => NewFile());
        fileContextMenu.Items.Add("پوشه جدید", null, (s, e) => NewFolder());
        fileContextMenu.Items.Add("حذف", null, (s, e) => DeleteFile());
        fileContextMenu.Items.Add("چسباندن", null, (s, e) => PasteFileOrFolder());
    }

    private void DeleteSelectedItem()
    {
        try
        {
            if (fileListBox.SelectedItem == null)
            {
                MessageBox.Show(text: "لطفاً یک فایل یا پوشه برای پاک کردن، مشخص کنید.", caption: "اخطار", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
                return;
            }

            string selectedItem = fileListBox.SelectedItem.ToString();
            string fullPath = Path.Combine(currentDirectory, selectedItem);

            DialogResult result = MessageBox.Show(text: $"اطمینان دارید؟ '{selectedItem}' آیا از حذف", caption: "تایید برای پاک کردن", buttons: MessageBoxButtons.YesNo, icon: MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }
                else if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                else
                {
                    MessageBox.Show(text: "موردی پیدا نشد!", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                    return;
                }

                UpdateFileListBox(currentDirectory);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در پاک کردن مورد: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void DeleteFile()
    {
        if (fileListBox.SelectedItem != null)
        {
            string fileName = fileListBox.SelectedItem.ToString();
            DeleteSelectedItem();
        }
    }

    private string clipboardPath = string.Empty;
    private bool isCutOperation = false;

    private void CopyFileOrFolder()
    {
        if (fileListBox.SelectedItem != null)
        {
            clipboardPath = Path.Combine(currentDirectory, fileListBox.SelectedItem.ToString());
            isCutOperation = false;
        }
    }

    private void CutFileOrFolder()
    {
        if (fileListBox.SelectedItem != null)
        {
            clipboardPath = Path.Combine(currentDirectory, fileListBox.SelectedItem.ToString());
            isCutOperation = true;
        }
    }

    private void PasteFileOrFolder()
    {
        if (string.IsNullOrEmpty(clipboardPath))
        {
            MessageBox.Show(text: "هیچ فایل یا پوشهی برای جایگذاری انتخاب نشده است.", caption: "خطا در جایگذاری", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
            return;
        }

        string destinationPath = Path.Combine(currentDirectory, Path.GetFileName(clipboardPath));

        try
        {
            if (File.Exists(clipboardPath))
            {
                if (isCutOperation)
                {
                    File.Move(clipboardPath, destinationPath);
                }
                else
                {
                    File.Copy(clipboardPath, destinationPath, true);
                }
            }
            else if (Directory.Exists(clipboardPath))
            {
                if (isCutOperation)
                {
                    Directory.Move(clipboardPath, destinationPath);
                }
                else
                {
                    DirectoryCopy(clipboardPath, destinationPath, true);
                }
            }
            else
            {
                MessageBox.Show(text: "مورد انتخاب شده دیگر وجود ندارد.", caption: "خطا در جایگذاری", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                return;
            }

            LoadFiles_Click(null, EventArgs.Empty);

            if (isCutOperation)
            {
                clipboardPath = string.Empty;
                isCutOperation = false;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا: {ex.Message}", caption: "خطا در جایگذاری", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private static void DirectoryCopy(string sourceDir, string destDir, bool copySubDirs)
    {
        DirectoryInfo dir = new DirectoryInfo(sourceDir);
        DirectoryInfo[] dirs = dir.GetDirectories();

        Directory.CreateDirectory(destDir);

        foreach (FileInfo file in dir.GetFiles())
        {
            string tempPath = Path.Combine(destDir, file.Name);
            file.CopyTo(tempPath, true);
        }

        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDir, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, true);
            }
        }
    }

    private void CreateNewFolder()
    {
        try
        {
            string newFolderName = "New Folder";
            string newFolderPath = Path.Combine(currentDirectory, newFolderName);

            int counter = 1;
            while (Directory.Exists(newFolderPath))
            {
                newFolderName = $"New Folder ({counter})";
                newFolderPath = Path.Combine(currentDirectory, newFolderName);
                counter++;
            }

            Directory.CreateDirectory(newFolderPath);

            UpdateFileListBox(currentDirectory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در ایجاد پوشه: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void CreateNewFile()
    {
        try
        {
            string newFileName = "New File.txt";
            string newFilePath = Path.Combine(currentDirectory, newFileName);

            int counter = 1;
            while (File.Exists(newFilePath))
            {
                newFileName = $"New File ({counter}).txt";
                newFilePath = Path.Combine(currentDirectory, newFileName);
                counter++;
            }

            File.WriteAllText(newFilePath, "");

            UpdateFileListBox(currentDirectory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در ایجاد فایل: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void NewFolder()
    {
        if (fileListBox.SelectedItem != null)
        {
            string fileName = fileListBox.SelectedItem.ToString();
        }

        CreateNewFolder();
    }

    public void Rename()
    {
        if (fileListBox.SelectedItem != null)
        {
            string oldName = fileListBox.SelectedItem.ToString();
            string oldPath = Path.Combine(currentDirectory, oldName);

            using (RenameForm renameForm = new RenameForm(oldName))
            {
                if (renameForm.ShowDialog() == DialogResult.OK)
                {
                    string newName = renameForm.Tag as string;

                    if (!string.IsNullOrEmpty(newName))
                    {
                        try
                        {
                            string newPath = Path.Combine(currentDirectory, newName);

                            if (Directory.Exists(oldPath))
                            {
                                Directory.Move(oldPath, newPath);
                            }
                            else if (File.Exists(oldPath))
                            {
                                File.Move(oldPath, newPath);
                            }

                            UpdateFileListBox(currentDirectory);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(text: "خطا: " + ex.Message, caption: "خطا در تغییر نام", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
                        }
                    }
                }
            }
        }
        else
        {
            MessageBox.Show(text: "لطفاً فایل یا پوشه مورد نظر برای تغییر نام را انتخاب کنید.", caption: "تغییر نام", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Warning);
        }
    }

    private void NewFile()
    {
        if (fileListBox.SelectedItem != null)
        {
            string fileName = fileListBox.SelectedItem.ToString();
        }

        CreateNewFile();
    }
    private void SaveFile_Click(object sender, EventArgs e)
    {
        try
        {
            if (!string.IsNullOrEmpty(currentFile))
            {
                File.WriteAllText(currentFile, fileEditor.Text);
                MessageBox.Show(text: "فایل ذخیره شد.", caption: "موفقیت", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در ذخیره فایل: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void BackButton_Click(object sender, EventArgs e)
    {
        currentDirectory = Directory.GetParent(currentDirectory)?.FullName ?? currentDirectory;
        LoadFiles_Click(sender, e);
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
            MessageBox.Show(text: $"خطا در باز کردن فایل: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void ClearButton_Click(object sender, EventArgs e)
    {
        fileEditor.Clear();
    }

    private void UploadFileOrFolder()
    {
        using (var ofd = new OpenFileDialog())
        {
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.Multiselect = false;
            ofd.Title = "یک فایل انتخاب کنید ویا برای انتخاب فولدر، کنسل کنید";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                CopyFile(ofd.FileName);
                return;
            }
        }

        using (var fbd = new FolderBrowserDialog())
        {
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                CopyFolder(fbd.SelectedPath);
            }
        }
    }

    private void CopyFile(string filePath)
    {
        string mainDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string fileName = Path.GetFileName(filePath);
        string destinationPath = Path.Combine(mainDirectory, fileName);

        try
        {
            File.Copy(filePath, destinationPath, true);
            fileListBox.Items.Add(destinationPath);
            MessageBox.Show(text: "فایل با موفقیت آپلود شد!", caption: "موفق", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در آپلود فایل: {ex.Message}", caption: "آپلود شکست خورد", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void CopyFolder(string sourceFolder)
    {
        string mainDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string folderName = Path.GetFileName(sourceFolder);
        string destinationFolder = Path.Combine(mainDirectory, folderName);

        try
        {
            DirectoryCopy(sourceFolder, destinationFolder, true);
            fileListBox.Items.Add(destinationFolder);
            MessageBox.Show(text: "پوشه با موفقیت آپلود شد!", caption: "موفق", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در آپلود پوشه: {ex.Message}", caption: "آپلود شکست خورد", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void RefreshButton_Click(object sender, EventArgs e)
    {
        UploadFileOrFolder();
    }

    private void InitializeFileWatcher()
    {
        if (fileWatcher != null)
        {
            fileWatcher.Dispose();
        }

        fileWatcher = new FileSystemWatcher
        {
            Path = currentDirectory,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite,
            Filter = "*.*",
            EnableRaisingEvents = true,
            IncludeSubdirectories = false
        };

        fileWatcher.Changed += OnFileSystemChanged;
        fileWatcher.Created += OnFileSystemChanged;
        fileWatcher.Deleted += OnFileSystemChanged;
        fileWatcher.Renamed += OnFileSystemRenamed;
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        Invoke(new Action(() => UpdateFileListBox(currentDirectory)));
    }

    private void OnFileSystemRenamed(object sender, RenamedEventArgs e)
    {
        Invoke(new Action(() => UpdateFileListBox(currentDirectory)));
    }


    private void UpdateFileListBox(string directoryPath)
    {
        try
        {
            fileListBox.Items.Clear();
            var files = Directory.GetFileSystemEntries(directoryPath);
            foreach (var file in files)
            {
                fileListBox.Items.Add(Path.GetFileName(file));
            }

            currentDirectory = directoryPath;

            InitializeFileWatcher();
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    //^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

    //***********************************************************************************
    // کد های مربوط به فرم
    //***********************************************************************************
    //===================================================================================
    private void DcbmForm_Load(object sender, EventArgs e)
    {
        CheckForUpdate();
        LoadFiles_Click();
        DashboardLabel_Load();
        this.KeyPreview = true;
        InitializeFileWatcher();
        howToManagePanel.Height = 0;
        whyDiscordBotPanel.Height = 0;
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
            MessageBox.Show(text: $"خطا در بارگذاری فایل‌ها: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
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
                string latestVersion = client.DownloadString(updateUrl + versionFile).Trim();
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
            MessageBox.Show(text: $"خطا در بررسی آپدیت: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
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
            MessageBox.Show(text: "دانلود فایل آپدیت با موفقیت انجام شد.", caption: "اطلاعیه", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(tempFilePath, extractPath);
            File.Delete(tempFilePath);
            MessageBox.Show(text: "استخراج فایل‌ها با موفقیت انجام شد.", caption: "اطلاعیه", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);

            File.WriteAllText(updaterScriptPath, $@"
@echo off
timeout /t 2 >nul
xcopy /s /y ""{extractPath}\*"" ""{Application.StartupPath}\""
start """" ""{Path.Combine(Application.StartupPath, Path.GetFileName(Application.ExecutablePath))}""
del /f /q ""%~f0""
rmdir /s /q ""{extractPath}""
exit
");

            MessageBox.Show(text: "اسکریپت آپدیت با موفقیت ایجاد شد.", caption: "اطلاعیه", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Information);

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
            MessageBox.Show(text: $"خطا در دانلود یا نصب آپدیت: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
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
    private int targetHeight = 141;
    private int step = 10;

    private async Task HowToManagePanelOpeningPanelAnimation()
    {
        while (howToManagePanel.Height < targetHeight)
        {
            howToManagePanel.Height += step;
            await Task.Delay(1);
        }
        howToManagePanel.Height = 141;
    }

    private async Task HowToManagePanelClosingPanelAnimation()
    {
        while (howToManagePanel.Height > 0)
        {
            howToManagePanel.Height -= step;
            await Task.Delay(1);
        }
        howToManagePanel.Height = 0;
    }

    private async void HowToManagePictureBox_Click(object sender, EventArgs e)
    {
        await HowToManagePanelOpeningPanelAnimation();
    }

    private async void HowToManagePanel_Click(object sender, EventArgs e)
    {
        await HowToManagePanelClosingPanelAnimation();
    }

    private async void HowToManageLabel_Click(object sender, EventArgs e)
    {
        await HowToManagePanelClosingPanelAnimation();
    }

    private async Task WhyDiscordBotPanelOpeningPanelAnimation()
    {
        while (whyDiscordBotPanel.Height < targetHeight)
        {
            whyDiscordBotPanel.Height += step;
            await Task.Delay(1);
        }
        whyDiscordBotPanel.Height = 141;
    }

    private async Task WhyDiscordBotPanelClosingPanelAnimation()
    {
        while (whyDiscordBotPanel.Height > 0)
        {
            whyDiscordBotPanel.Height -= step;
            await Task.Delay(1);
        }
        whyDiscordBotPanel.Height = 0;
    }

    private async void WhyDiscordBotPictureBox_Click(object sender, EventArgs e)
    {
        await WhyDiscordBotPanelOpeningPanelAnimation();
    }

    private async void WhyDiscordBotPanel_Click(object sender, EventArgs e)
    {
        await WhyDiscordBotPanelClosingPanelAnimation();
    }

    private async void WhyDiscordBotLabel_Click(object sender, EventArgs e)
    {
        await WhyDiscordBotPanelClosingPanelAnimation();
    }

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
        if (e.Control == true && e.Alt == true && e.Shift == true && e.KeyCode.ToString() == "Q" || e.Control == true && e.Alt == true && e.Shift == true && e.KeyCode.ToString() == "E")
        {
            Application.Exit();
            //using (var folderBrowserDialog = new FolderBrowserDialog())
            //{
            //    folderBrowserDialog.Description = "یک دایرکتوری انتخاب کنید:";
            //    folderBrowserDialog.ShowNewFolderButton = false;

            //    if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            //    {
            //        currentDirectory = folderBrowserDialog.SelectedPath;

            //        LoadFiles_Click(sender, e);
            //    }
            //}
        }
    }

    private void MaximizePictureBox_Click(object sender, EventArgs e)
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

    private void LinkLabel_Click(object sender, EventArgs e)
    {
        try
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                FileName = "https://overtm.ir/setup-discordbot",
                UseShellExecute = true,
            };

            Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            MessageBox.Show(text: $"خطا در باز کردن لینک: {ex.Message}", caption: "خطا", buttons: MessageBoxButtons.OK, icon: MessageBoxIcon.Error);
        }
    }

    private void UploadLabel_Click(object sender, EventArgs e)
    {
        UploadFileOrFolder();
    }
    //|||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||||
}
