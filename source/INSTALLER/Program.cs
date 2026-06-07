using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace PythonCoderGame.Setup;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        try
        {
            ApplicationConfiguration.Initialize();
            var mode = args.Any(a => a.Equals("/uninstall", StringComparison.OrdinalIgnoreCase))
                ? SetupMode.Uninstall
                : SetupMode.InstallOrUpdate;
            Application.Run(new SetupWizardForm(mode));
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "PythonCoderGame.Setup.error.log");
            File.WriteAllText(logPath, ex.ToString());
            MessageBox.Show(
                $"Python Coder Game Setup could not start. Details written to:{Environment.NewLine}{logPath}{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Setup startup failed",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

internal enum SetupMode
{
    InstallOrUpdate,
    CleanInstall,
    Uninstall
}

internal sealed class SetupWizardForm : Form
{
    private const string AppName = "Python Coder Game";
    private const string ExeName = "PythonCoderGame.exe";
    private const string SetupExeName = "PythonCoderGame.Setup.exe";
    private const string Version = "1.0.0";
    private const string MarkerFile = ".python-coder-game-install";
    private const string AppRegistryPath = @"SOFTWARE\PythonCoderGame";
    private const string UninstallRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PythonCoderGame";
    private const string PayloadResourceName = "PythonCoderGame.Payload.zip";

    private readonly List<Control> _pages = new();
    private readonly Panel _host = new() { Dock = DockStyle.Fill, Padding = new Padding(28, 24, 28, 18) };
    private readonly Panel _header = new() { Dock = DockStyle.Top, Height = 92 };
    private readonly Label _title = new() { AutoSize = false, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _subtitle = new() { AutoSize = false, Dock = DockStyle.Bottom, Height = 24, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Button _back = new() { Text = "Back", Width = 112, Height = 38 };
    private readonly Button _next = new() { Text = "Next", Width = 112, Height = 38 };
    private readonly Button _cancel = new() { Text = "Cancel", Width = 112, Height = 38 };
    private readonly SetupState _state;

    private int _pageIndex;
    private bool _completed;

    private readonly Color _bg = Color.FromArgb(5, 7, 14);
    private readonly Color _panel = Color.FromArgb(10, 14, 28);
    private readonly Color _panel2 = Color.FromArgb(16, 18, 38);
    private readonly Color _cyan = Color.FromArgb(0, 245, 255);
    private readonly Color _magenta = Color.FromArgb(255, 46, 185);
    private readonly Color _green = Color.FromArgb(64, 255, 160);
    private readonly Color _orange = Color.FromArgb(255, 174, 54);
    private readonly Color _text = Color.FromArgb(242, 247, 255);
    private readonly Color _muted = Color.FromArgb(168, 190, 225);

    public SetupWizardForm(SetupMode requestedMode)
    {
        _state = new SetupState(requestedMode)
        {
            InstallDir = GetExistingInstallDir()
        };
        if (string.IsNullOrWhiteSpace(_state.InstallDir))
        {
            _state.InstallDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python Coder Game");
        }

        Text = "Python Coder Game // Cyberpunk Arcade Installer";
        Width = 920;
        Height = 640;
        MinimumSize = new Size(840, 560);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = _bg;
        ForeColor = _text;

        _header.BackColor = _bg;
        _header.Padding = new Padding(28, 14, 28, 8);
        _title.Text = "PYTHON CODER // INSTALL TERMINAL";
        _title.Font = new Font("Consolas", 24, FontStyle.Bold);
        _title.ForeColor = _cyan;
        _subtitle.Text = "guided install / update / clean install / uninstall";
        _subtitle.Font = new Font("Consolas", 10, FontStyle.Bold);
        _subtitle.ForeColor = _magenta;
        _header.Controls.Add(_title);
        _header.Controls.Add(_subtitle);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 68,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(16),
            BackColor = Color.FromArgb(8, 8, 18)
        };
        StyleButton(_cancel, _magenta);
        StyleButton(_next, _green);
        StyleButton(_back, _cyan);
        footer.Controls.Add(_cancel);
        footer.Controls.Add(_next);
        footer.Controls.Add(_back);

        Controls.Add(_host);
        Controls.Add(footer);
        Controls.Add(_header);

        _back.Click += (_, _) => MovePage(-1);
        _next.Click += (_, _) => Next();
        _cancel.Click += (_, _) => Close();

        BuildPages();
        ShowPage(0);
    }

    private void BuildPages()
    {
        var existing = !string.IsNullOrWhiteSpace(GetExistingInstallDir());
        if (_state.Mode == SetupMode.Uninstall)
        {
            _pages.Add(InstallPage("Uninstall Console"));
            _pages.Add(FinishedPage());
            return;
        }

        if (existing)
        {
            _pages.Add(ExistingInstallPage());
        }

        _pages.Add(WelcomePage());
        _pages.Add(FolderSelectionPage());
        _pages.Add(InstallPage("Install Console"));
        _pages.Add(FinishedPage());
    }

    private Control ExistingInstallPage()
    {
        var page = BasePage("EXISTING INSTALL DETECTED");
        page.Name = "ExistingInstallPage";

        page.Controls.Add(BodyText($"A previous Python Coder Game installation was found at:{Environment.NewLine}{_state.InstallDir}{Environment.NewLine}{Environment.NewLine}Choose how the setup terminal should proceed."));

        var update = ModeRadio("Update / reinstall application files", SetupMode.InstallOrUpdate, true);
        var clean = ModeRadio("Clean install: replace app files and remove local student profiles/telemetry", SetupMode.CleanInstall, false);
        var uninstall = ModeRadio("Uninstall Python Coder Game from this system", SetupMode.Uninstall, false);
        page.Controls.Add(update);
        page.Controls.Add(clean);
        page.Controls.Add(uninstall);

        return page;
    }

    private RadioButton ModeRadio(string text, SetupMode mode, bool selected)
    {
        var radio = new RadioButton
        {
            Text = text,
            Checked = _state.Mode == mode || selected && _state.Mode == SetupMode.InstallOrUpdate,
            Width = 780,
            Height = 34,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = mode == SetupMode.Uninstall ? _orange : _text,
            BackColor = _bg,
            Margin = new Padding(0, 6, 0, 6)
        };
        radio.CheckedChanged += (_, _) =>
        {
            if (radio.Checked)
            {
                _state.Mode = mode;
            }
        };
        return radio;
    }

    private Control WelcomePage()
    {
        var page = BasePage("WELCOME, OPERATOR");
        page.Name = "WelcomePage";

        page.Controls.Add(BodyText(
            "This wizard installs the Python Coder Game arcade learning terminal. It will stage the Windows x64 game executable, music resources, shortcuts, and Add/Remove Programs registry entries." +
            Environment.NewLine + Environment.NewLine +
            "Student profiles and telemetry live in AppData and are preserved during normal updates. Choose clean install only when you intentionally want to wipe local learning data."));

        page.Controls.Add(StatusPill("PAYLOAD", HasEmbeddedPayload() ? "embedded and ready" : "missing"));
        page.Controls.Add(StatusPill("REGISTRY", @"HKLM\SOFTWARE\PythonCoderGame"));
        page.Controls.Add(StatusPill("UNINSTALL", "registered with Windows Apps & Features"));

        return page;
    }

    private Control FolderSelectionPage()
    {
        var page = BasePage("DESTINATION DIRECTORY");
        page.Name = "FolderSelectionPage";

        page.Controls.Add(BodyText("Choose where the game files should be installed. The default location is recommended for classroom and lab machines."));

        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Width = 800,
            Height = 46,
            BackColor = _bg,
            Margin = new Padding(0, 8, 0, 22)
        };

        var dirText = new TextBox
        {
            Text = _state.InstallDir,
            Width = 610,
            Font = new Font("Consolas", 11),
            BackColor = _panel2,
            ForeColor = _text,
            BorderStyle = BorderStyle.FixedSingle
        };
        dirText.TextChanged += (_, _) => _state.InstallDir = dirText.Text.Trim();

        var browse = new Button { Text = "Browse", Width = 116, Height = 34 };
        StyleButton(browse, _cyan);
        browse.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = Directory.Exists(dirText.Text) ? dirText.Text : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                dirText.Text = dialog.SelectedPath;
            }
        };

        row.Controls.Add(dirText);
        row.Controls.Add(browse);
        page.Controls.Add(row);

        var desktop = Toggle("Create desktop shortcut", _state.CreateDesktopShortcut);
        desktop.CheckedChanged += (_, _) => _state.CreateDesktopShortcut = desktop.Checked;
        var startMenu = Toggle("Create Start Menu shortcut", _state.CreateStartMenuShortcut);
        startMenu.CheckedChanged += (_, _) => _state.CreateStartMenuShortcut = startMenu.Checked;
        var cleanData = Toggle("Clean local student profiles and telemetry in AppData", _state.Mode == SetupMode.CleanInstall);
        cleanData.CheckedChanged += (_, _) =>
        {
            _state.CleanStudentData = cleanData.Checked;
            if (cleanData.Checked)
            {
                _state.Mode = SetupMode.CleanInstall;
            }
        };

        page.Controls.Add(desktop);
        page.Controls.Add(startMenu);
        page.Controls.Add(cleanData);

        return page;
    }

    private Control InstallPage(string header)
    {
        var page = BasePage(header.ToUpperInvariant());
        page.Name = "InstallPage";

        var progress = new ProgressBar
        {
            Width = 790,
            Height = 22,
            Style = ProgressBarStyle.Continuous,
            ForeColor = _green,
            Margin = new Padding(0, 0, 0, 14)
        };

        var log = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Width = 790,
            Height = 330,
            Font = new Font("Consolas", 10),
            BackColor = Color.FromArgb(2, 4, 10),
            ForeColor = _green,
            BorderStyle = BorderStyle.FixedSingle
        };

        page.Controls.Add(progress);
        page.Controls.Add(log);
        page.Tag = new InstallControls(log, progress);
        return page;
    }

    private Control FinishedPage()
    {
        var page = BasePage("SEQUENCE COMPLETE");
        page.Name = "FinishedPage";

        var text = _state.Mode == SetupMode.Uninstall
            ? "Python Coder Game has been removed from Windows registry, shortcuts, and installation files."
            : "Python Coder Game is ready. The installer has registered the app with Windows and copied an uninstaller into the install directory.";
        page.Controls.Add(BodyText(text));

        var launch = Toggle("Launch Python Coder Game now", _state.LaunchAfterInstall);
        launch.Visible = _state.Mode != SetupMode.Uninstall;
        launch.CheckedChanged += (_, _) => _state.LaunchAfterInstall = launch.Checked;
        page.Controls.Add(launch);
        page.Tag = launch;
        return page;
    }

    private FlowLayoutPanel BasePage(string header)
    {
        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = _bg,
            Padding = new Padding(10)
        };

        layout.Controls.Add(new Label
        {
            Text = header,
            AutoSize = true,
            Font = new Font("Consolas", 18, FontStyle.Bold),
            ForeColor = _green,
            Margin = new Padding(0, 0, 0, 16)
        });

        return layout;
    }

    private Label BodyText(string text) => new()
    {
        Text = text,
        Width = 790,
        Height = 116,
        Font = new Font("Segoe UI", 10),
        ForeColor = _muted,
        BackColor = _bg,
        Margin = new Padding(0, 0, 0, 14)
    };

    private Label StatusPill(string label, string value) => new()
    {
        Text = $"{label,-12} :: {value}",
        Width = 790,
        Height = 34,
        Font = new Font("Consolas", 10, FontStyle.Bold),
        ForeColor = _cyan,
        BackColor = _panel,
        BorderStyle = BorderStyle.FixedSingle,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(12, 0, 0, 0),
        Margin = new Padding(0, 5, 0, 5)
    };

    private CheckBox Toggle(string text, bool selected) => new()
    {
        Text = text,
        Checked = selected,
        Width = 790,
        Height = 32,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        ForeColor = _text,
        BackColor = _bg,
        Margin = new Padding(0, 4, 0, 4)
    };

    private void StyleButton(Button button, Color border)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.BackColor = _panel;
        button.ForeColor = _text;
        button.Font = new Font("Consolas", 10, FontStyle.Bold);
        button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.BorderSize = 1;
        button.Cursor = Cursors.Hand;
    }

    private void MovePage(int delta) => ShowPage(Math.Clamp(_pageIndex + delta, 0, _pages.Count - 1));

    private void ShowPage(int index)
    {
        _pageIndex = index;
        _host.Controls.Clear();
        _host.Controls.Add(_pages[index]);
        _subtitle.Text = $"stage {index + 1:00}/{_pages.Count:00} // {_state.Mode.ToString().ToUpperInvariant()}";
        _back.Enabled = index > 0 && !_completed;
        _next.Text = _pages[index].Name switch
        {
            "InstallPage" when _state.Mode == SetupMode.Uninstall => "Uninstall",
            "InstallPage" => "Install",
            "FinishedPage" => "Finish",
            _ => "Next"
        };
        _cancel.Enabled = !_completed || _pages[index].Name == "FinishedPage";
    }

    private void Next()
    {
        if (_pageIndex == _pages.Count - 1)
        {
            if (_state.LaunchAfterInstall && _state.Mode != SetupMode.Uninstall)
            {
                var exe = Path.Combine(_state.InstallDir, ExeName);
                if (File.Exists(exe))
                {
                    Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true, WorkingDirectory = _state.InstallDir });
                }
            }
            Close();
            return;
        }

        if (_pages[_pageIndex].Name == "ExistingInstallPage" && _state.Mode == SetupMode.Uninstall)
        {
            ShowInstallAndExecute();
            return;
        }

        if (_pages[_pageIndex].Name == "FolderSelectionPage")
        {
            if (string.IsNullOrWhiteSpace(_state.InstallDir))
            {
                MessageBox.Show(this, "Choose a destination directory first.", "Install validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        if (_pages[_pageIndex].Name == "InstallPage")
        {
            ExecuteCurrentOperation((InstallControls)_pages[_pageIndex].Tag!);
            return;
        }

        MovePage(1);
    }

    private void ShowInstallAndExecute()
    {
        var installIndex = _pages.FindIndex(p => p.Name == "InstallPage");
        ShowPage(installIndex);
        ExecuteCurrentOperation((InstallControls)_pages[installIndex].Tag!);
    }

    private void ExecuteCurrentOperation(InstallControls controls)
    {
        _next.Enabled = false;
        _back.Enabled = false;
        _cancel.Enabled = false;
        controls.Log.Clear();
        controls.Progress.Value = 0;

        try
        {
            if (_state.Mode == SetupMode.Uninstall)
            {
                ExecuteUninstall(controls);
            }
            else
            {
                ExecuteInstall(controls);
            }

            _completed = true;
            _next.Enabled = true;
            _next.Text = "Finish";
            MovePage(1);
        }
        catch (Exception ex)
        {
            Log(controls.Log, $"ERROR: {ex.Message}");
            Log(controls.Log, ex.StackTrace ?? "");
            _next.Enabled = true;
            _next.Text = "Close";
            _cancel.Enabled = true;
            MessageBox.Show(this, ex.Message, "Setup failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExecuteInstall(InstallControls controls)
    {
        Log(controls.Log, $"Starting {AppName} install sequence.");
        Log(controls.Log, $"Target: {_state.InstallDir}");
        SetProgress(controls.Progress, 5);

        StopRunningGame(controls.Log);
        SetProgress(controls.Progress, 15);

        if (_state.Mode == SetupMode.CleanInstall || _state.CleanStudentData)
        {
            CleanStudentData(controls.Log);
        }

        Directory.CreateDirectory(_state.InstallDir);
        ExtractPayload(controls);
        SetProgress(controls.Progress, 72);

        File.WriteAllText(Path.Combine(_state.InstallDir, MarkerFile), $"Installed {DateTime.Now:O}{Environment.NewLine}");
        CopySetupIntoInstallDir(controls.Log);
        CreateShortcuts(controls.Log);
        SetProgress(controls.Progress, 86);

        RegisterApp(controls.Log);
        SetProgress(controls.Progress, 100);
        Log(controls.Log, "Install sequence complete.");
        MessageBox.Show(this, $"{AppName} is installed and registered with Windows.", "Install complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExecuteUninstall(InstallControls controls)
    {
        if (string.IsNullOrWhiteSpace(_state.InstallDir))
        {
            _state.InstallDir = GetExistingInstallDir();
        }

        Log(controls.Log, $"Starting {AppName} uninstall sequence.");
        Log(controls.Log, $"Target: {_state.InstallDir}");
        SetProgress(controls.Progress, 10);

        StopRunningGame(controls.Log);
        RemoveShortcuts(controls.Log);
        SetProgress(controls.Progress, 35);

        RemoveRegistry(controls.Log);
        SetProgress(controls.Progress, 55);

        RemoveInstallDirectory(controls.Log);
        SetProgress(controls.Progress, 100);
        Log(controls.Log, "Uninstall sequence complete.");
        MessageBox.Show(this, $"{AppName} has been uninstalled.", "Uninstall complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExtractPayload(InstallControls controls)
    {
        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(PayloadResourceName)
            ?? throw new InvalidOperationException("Embedded payload is missing. Run INSTALLER\\build-installer.ps1 to create the packed installer.");
        using var archive = new ZipArchive(payload, ZipArchiveMode.Read);
        var total = Math.Max(archive.Entries.Count, 1);
        var count = 0;
        Log(controls.Log, $"Extracting {archive.Entries.Count} payload entries.");

        foreach (var entry in archive.Entries)
        {
            var dest = Path.GetFullPath(Path.Combine(_state.InstallDir, entry.FullName));
            var root = Path.GetFullPath(_state.InstallDir);
            if (!dest.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                Log(controls.Log, $"Skipped unsafe payload entry: {entry.FullName}");
                continue;
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(dest);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                entry.ExtractToFile(dest, overwrite: true);
            }

            count++;
            if (count % 5 == 0 || count == total)
            {
                SetProgress(controls.Progress, 15 + (int)(55.0 * count / total));
                Log(controls.Log, $"Extracted {count}/{total}: {entry.FullName}");
                Application.DoEvents();
            }
        }
    }

    private void CopySetupIntoInstallDir(TextBox log)
    {
        var target = Path.Combine(_state.InstallDir, SetupExeName);
        var current = Application.ExecutablePath;
        if (!string.Equals(Path.GetFullPath(current), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
        {
            File.Copy(current, target, overwrite: true);
        }
        Log(log, $"Setup maintenance executable staged: {target}");
    }

    private void CreateShortcuts(TextBox log)
    {
        var exe = Path.Combine(_state.InstallDir, ExeName);
        if (!File.Exists(exe))
        {
            Log(log, "Game executable was not found; shortcut creation skipped.");
            return;
        }

        if (_state.CreateDesktopShortcut)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppName}.lnk");
            CreateShortcut(path, exe, _state.InstallDir, "Launch Python Coder Game");
            Log(log, $"Desktop shortcut created: {path}");
        }

        if (_state.CreateStartMenuShortcut)
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName);
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, $"{AppName}.lnk");
            CreateShortcut(path, exe, _state.InstallDir, "Launch Python Coder Game");
            Log(log, $"Start Menu shortcut created: {path}");
        }
    }

    private void RemoveShortcuts(TextBox log)
    {
        var desktop = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{AppName}.lnk");
        if (File.Exists(desktop))
        {
            File.Delete(desktop);
            Log(log, $"Deleted desktop shortcut: {desktop}");
        }

        var start = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), AppName);
        if (Directory.Exists(start))
        {
            Directory.Delete(start, recursive: true);
            Log(log, $"Deleted Start Menu folder: {start}");
        }
    }

    private void RegisterApp(TextBox log)
    {
        var exe = Path.Combine(_state.InstallDir, ExeName);
        var setup = Path.Combine(_state.InstallDir, SetupExeName);
        using var appKey = Registry.LocalMachine.CreateSubKey(AppRegistryPath);
        appKey?.SetValue("InstallDir", _state.InstallDir);
        appKey?.SetValue("Version", Version);
        appKey?.SetValue("InstalledOn", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

        using var uninstall = Registry.LocalMachine.CreateSubKey(UninstallRegistryPath);
        uninstall?.SetValue("DisplayName", AppName);
        uninstall?.SetValue("DisplayVersion", Version);
        uninstall?.SetValue("Publisher", "Antigrav");
        uninstall?.SetValue("InstallLocation", _state.InstallDir);
        uninstall?.SetValue("DisplayIcon", $"{exe},0");
        uninstall?.SetValue("UninstallString", $"\"{setup}\" /uninstall");
        uninstall?.SetValue("QuietUninstallString", $"\"{setup}\" /uninstall");
        uninstall?.SetValue("NoModify", 1, RegistryValueKind.DWord);
        uninstall?.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        uninstall?.SetValue("EstimatedSize", EstimateInstallSizeKb(_state.InstallDir), RegistryValueKind.DWord);
        uninstall?.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));

        Log(log, @"Registry updated: HKLM\SOFTWARE\PythonCoderGame");
        Log(log, @"Registry updated: HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\PythonCoderGame");
    }

    private void RemoveRegistry(TextBox log)
    {
        Registry.LocalMachine.DeleteSubKeyTree(UninstallRegistryPath, throwOnMissingSubKey: false);
        Registry.LocalMachine.DeleteSubKeyTree(AppRegistryPath, throwOnMissingSubKey: false);
        Log(log, "Registry uninstall entries removed.");
    }

    private void RemoveInstallDirectory(TextBox log)
    {
        if (string.IsNullOrWhiteSpace(_state.InstallDir) || !Directory.Exists(_state.InstallDir))
        {
            Log(log, "Install directory is already absent.");
            return;
        }

        var marker = Path.Combine(_state.InstallDir, MarkerFile);
        if (!File.Exists(marker))
        {
            Log(log, $"Safety marker missing; install directory was not deleted: {_state.InstallDir}");
            return;
        }

        var currentExe = Path.GetFullPath(Application.ExecutablePath);
        foreach (var file in Directory.GetFiles(_state.InstallDir, "*", SearchOption.AllDirectories))
        {
            var full = Path.GetFullPath(file);
            if (full.Equals(currentExe, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.SetAttributes(full, FileAttributes.Normal);
            File.Delete(full);
        }

        foreach (var dir in Directory.GetDirectories(_state.InstallDir, "*", SearchOption.AllDirectories).OrderByDescending(d => d.Length))
        {
            if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
            {
                Directory.Delete(dir);
            }
        }

        try
        {
            Directory.Delete(_state.InstallDir, recursive: false);
        }
        catch
        {
            if (currentExe.StartsWith(Path.GetFullPath(_state.InstallDir), StringComparison.OrdinalIgnoreCase))
            {
                MoveFileEx(currentExe, null, 0x00000004);
                Log(log, "Setup executable is currently running and is scheduled for removal on reboot.");
            }
        }
    }

    private void CleanStudentData(TextBox log)
    {
        var data = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PythonCoderGame");
        if (Directory.Exists(data))
        {
            Directory.Delete(data, recursive: true);
            Log(log, $"Clean install removed local student data: {data}");
        }
        else
        {
            Log(log, "No local student AppData found to clean.");
        }
    }

    private static void StopRunningGame(TextBox log)
    {
        foreach (var proc in Process.GetProcessesByName("PythonCoderGame"))
        {
            try
            {
                log.AppendText($"[{DateTime.Now:HH:mm:ss}] Stopping PythonCoderGame process {proc.Id}.{Environment.NewLine}");
                proc.Kill(entireProcessTree: true);
                proc.WaitForExit(3000);
            }
            catch
            {
                // Best effort; extraction may still fail if Windows keeps the process locked.
            }
        }
    }

    private static string GetExistingInstallDir()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(AppRegistryPath);
            return key?.GetValue("InstallDir") as string ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static bool HasEmbeddedPayload() =>
        Assembly.GetExecutingAssembly().GetManifestResourceNames().Contains(PayloadResourceName, StringComparer.Ordinal);

    private static int EstimateInstallSizeKb(string dir)
    {
        if (!Directory.Exists(dir))
        {
            return 0;
        }

        var bytes = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
        return Math.Max(1, (int)(bytes / 1024));
    }

    private static void Log(TextBox box, string text)
    {
        box.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        box.SelectionStart = box.TextLength;
        box.ScrollToCaret();
        Application.DoEvents();
    }

    private static void SetProgress(ProgressBar bar, int value)
    {
        bar.Value = Math.Clamp(value, bar.Minimum, bar.Maximum);
        Application.DoEvents();
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory, string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("WScript.Shell is not available.");
        var shell = Activator.CreateInstance(shellType);
        var shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
        var shortcutType = shortcut?.GetType() ?? throw new InvalidOperationException("Failed to create shortcut.");

        shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
        shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
        shortcutType.InvokeMember("Description", BindingFlags.SetProperty, null, shortcut, new object[] { description });
        shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { $"{targetPath},0" });
        shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, Array.Empty<object>());

        if (shortcut != null && Marshal.IsComObject(shortcut))
        {
            Marshal.FinalReleaseComObject(shortcut);
        }
        if (shell != null && Marshal.IsComObject(shell))
        {
            Marshal.FinalReleaseComObject(shell);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool MoveFileEx(string existingFileName, string? newFileName, int flags);

    private sealed record InstallControls(TextBox Log, ProgressBar Progress);
}

internal sealed class SetupState(SetupMode mode)
{
    public SetupMode Mode { get; set; } = mode;
    public string InstallDir { get; set; } = "";
    public bool CreateDesktopShortcut { get; set; } = true;
    public bool CreateStartMenuShortcut { get; set; } = true;
    public bool CleanStudentData { get; set; }
    public bool LaunchAfterInstall { get; set; } = true;
}
