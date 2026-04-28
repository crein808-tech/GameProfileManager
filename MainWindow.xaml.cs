using System.IO;
using System.Windows;
using System.Windows.Controls;
using GameProfileManager.Models;
using GameProfileManager.Services;

namespace GameProfileManager;

public partial class MainWindow : Window
{
    private static readonly string BackupBaseDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GameProfileManager", "backups");

    private static readonly string FgCacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GameProfileManager", "fg_cache");

    private readonly AppSettings _settings;
    private readonly NpiService _npi;
    private readonly DllSwapService _dllSwap;
    private readonly BackupService _backups;
    private readonly DlssTweaksService _tweaks;
    private readonly FgOverrideService _fgOverride;
    private GpuInfo? _gpu;
    private DllManifest? _manifest;
    private List<GameEntry> _allGames = [];
    private List<GameEntry> _manualGames = [];
    private List<DetectedDll> _detectedDlls = [];
    private GameEntry? _selectedGame;
    private bool _suppressSelectionChanged;

    public MainWindow()
    {
        _settings = SettingsService.Load();
        _npi = new NpiService(_settings.NpiExePath);
        _dllSwap = new DllSwapService(BackupBaseDir);
        _backups = new BackupService(BackupBaseDir);
        _tweaks = new DlssTweaksService(_settings.DlssTweaksDll, _settings.DlssTweaksIni);
        _fgOverride = new FgOverrideService(FgCacheDir);
        InitializeComponent();
        RestoreWindowState();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void RestoreWindowState()
    {
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;
        if (!double.IsNaN(_settings.WindowLeft) && !double.IsNaN(_settings.WindowTop))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
        if (_settings.IsMaximized)
            WindowState = WindowState.Maximized;
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _settings.IsMaximized = WindowState == WindowState.Maximized;
        if (WindowState == WindowState.Normal)
        {
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
            _settings.WindowLeft = Left;
            _settings.WindowTop = Top;
        }

        _settings.ManualGames = _manualGames.Select(g => new ManualGameEntry
        {
            Name = g.Name,
            InstallDir = g.InstallDir
        }).ToList();

        SettingsService.Save(_settings);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "Detecting hardware & games...";
        _gpu = await Task.Run(HardwareDetector.DetectGpu);
        _manualGames = _settings.ManualGames.Select(m => new GameEntry
        {
            Name = m.Name,
            InstallDir = m.InstallDir,
            Source = GameSource.Manual
        }).ToList();
        _allGames = SteamDetectionService.DetectGames().Concat(_manualGames).OrderBy(g => g.Name).ToList();
        RefreshGameList();
        StatusText.Text = _gpu is not null
            ? $"{_allGames.Count} games  •  {_gpu}"
            : $"{_allGames.Count} games detected";

        if (_gpu is not null && !_gpu.IsNvidia)
        {
            var vendorNote = $"Detected GPU: {_gpu.Name} ({_gpu.Vendor})\n\n";
            NpiPanel.Children.Insert(0, CreateVendorWarning(
                vendorNote + "NVIDIA Profile Inspector requires an NVIDIA GPU. " +
                "This tab's features will not work with your current hardware."));
            TweaksPanel.Children.Insert(0, CreateVendorWarning(
                vendorNote + "DLSSTweaks requires NVIDIA DLSS. " +
                "This tab's features will not work with your current hardware.\n\n" +
                "DLL Swap still works — you can swap FSR and XeSS DLLs on any GPU."));
        }

        if (!_npi.IsAvailable)
            NpiCurrentProfile.Text = $"NPI not found at:\n{_settings.NpiExePath}";

        try
        {
            _manifest = await ManifestService.FetchManifestAsync();
            ManifestStatus.Text = $"{_manifest.TotalCount} DLLs available";
        }
        catch
        {
            ManifestStatus.Text = "Manifest unavailable";
        }

        PopulateFgVersionCombo();
    }

    private void RefreshGameList(string? filter = null)
    {
        _suppressSelectionChanged = true;
        var previousSelection = _selectedGame;

        GameList.Items.Clear();
        var filtered = string.IsNullOrWhiteSpace(filter)
            ? _allGames
            : _allGames.Where(g => g.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var game in filtered)
            GameList.Items.Add(game);

        if (previousSelection is not null && filtered.Contains(previousSelection))
            GameList.SelectedItem = previousSelection;

        _suppressSelectionChanged = false;
    }

    private void GameSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshGameList(GameSearchBox.Text);
    }

    private async void GameList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionChanged)
            return;

        _selectedGame = GameList.SelectedItem as GameEntry;
        if (_selectedGame is null)
            return;

        NpiCurrentProfile.Text = $"Game: {_selectedGame.Name}\nPath: {_selectedGame.InstallDir}";
        StatusText.Text = $"Selected: {_selectedGame.Name}";
        UpdateTweaksStatus();

        await ScanSelectedGameDllsAsync();
        RefreshBackupsList();
    }

    private void RefreshBackupsList()
    {
        BackupsList.Items.Clear();
        if (_selectedGame is null)
            return;

        var all = _backups.GetAllBackups(_selectedGame);
        foreach (var entry in all)
            BackupsList.Items.Add(entry);
    }

    private async Task ScanSelectedGameDllsAsync(DllType? preserveSelectedType = null)
    {
        if (_selectedGame is null)
            return;

        var scanTarget = _selectedGame;
        StatusText.Text = "Scanning game directory...";
        var gameDir = scanTarget.InstallDir;
        var detected = await Task.Run(() => DllDetectionService.ScanGameDirectory(gameDir));

        if (_selectedGame != scanTarget)
            return;
        _detectedDlls = detected;

        if (detected.Count == 0)
        {
            DllDetectedList.Text = "No DLSS/FSR/XeSS DLLs found in this game directory.";
            DllTypeCombo.ItemsSource = null;
            DllTypeCombo.IsEnabled = false;
            DllVersionCombo.ItemsSource = null;
            DllVersionCombo.IsEnabled = false;
            DllBackupStatus.Text = "No DLLs to back up.";
            StatusText.Text = $"Selected: {_selectedGame.Name}";
            return;
        }

        var lines = detected.Select(d =>
        {
            var typeName = DllTypeMap.Filenames[d.Type];
            var sizeMb = d.FileSize / (1024.0 * 1024.0);
            return $"{typeName}  v{d.Version}  ({sizeMb:F1} MB)\n  {d.RelativePath}";
        });
        DllDetectedList.Text = string.Join("\n\n", lines);

        var items = detected.Select(d => new DllTypeComboItem(d)).ToList();
        DllTypeCombo.ItemsSource = items;
        DllTypeCombo.DisplayMemberPath = "Display";
        DllTypeCombo.IsEnabled = true;

        var restoreIndex = 0;
        if (preserveSelectedType is not null)
        {
            var idx = items.FindIndex(i => i.Detected.Type == preserveSelectedType);
            if (idx >= 0) restoreIndex = idx;
        }
        DllTypeCombo.SelectedIndex = restoreIndex;

        UpdateDllBackupStatus();
        StatusText.Text = $"Selected: {_selectedGame.Name}";
    }

    private void DllTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        DllTypeDesc.Text = DllTypeCombo.SelectedItem is DllTypeComboItem selected
            && DllTypeMap.Descriptions.TryGetValue(selected.Detected.Type, out var desc)
                ? desc : "";
        PopulateVersionCombo();
    }

    private void DllVersionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DllVersionCombo.SelectedItem is not DllVersionComboItem selected)
        {
            DllVersionDesc.Text = "";
            return;
        }

        var r = selected.Record;
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(r.FileDescription))
            parts.Add(r.FileDescription);
        if (!string.IsNullOrEmpty(r.SignedDatetime))
            parts.Add($"Signed: {r.SignedDatetime}");
        if (!string.IsNullOrEmpty(r.DllSource))
            parts.Add($"Source: {r.DllSource}");
        DllVersionDesc.Text = parts.Count > 0 ? string.Join("  •  ", parts) : "";
    }

    private void PopulateVersionCombo()
    {
        if (_manifest is null || DllTypeCombo.SelectedItem is not DllTypeComboItem selected)
        {
            DllVersionCombo.ItemsSource = null;
            DllVersionCombo.IsEnabled = false;
            return;
        }

        var sorted = _manifest.GetRecords(selected.Detected.Type)
            .Where(r => !r.IsDevFile)
            .OrderByDescending(r => r.VersionNumber)
            .ToList();
        var records = sorted.Select((r, i) => new DllVersionComboItem(r, IsRecommended: i == 0))
            .ToList();

        DllVersionCombo.ItemsSource = records;
        DllVersionCombo.DisplayMemberPath = "Display";
        DllVersionCombo.IsEnabled = records.Count > 0;
        if (records.Count > 0)
            DllVersionCombo.SelectedIndex = 0;
    }

    private void UpdateDllBackupStatus()
    {
        if (_selectedGame is null || _detectedDlls.Count == 0)
        {
            DllBackupStatus.Text = "No DLLs to back up.";
            return;
        }

        var backed = _detectedDlls.Count(d => _dllSwap.HasBackup(_selectedGame, d));
        DllBackupStatus.Text = backed == 0
            ? "No backups yet. Originals will be backed up before first swap."
            : $"{backed}/{_detectedDlls.Count} original DLL(s) backed up.";
    }

    private void AddGame_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Select Game Folder"
        };

        if (dialog.ShowDialog() == true)
        {
            var name = Path.GetFileName(dialog.FolderName) ?? "Unknown";
            var game = new GameEntry
            {
                Name = name,
                InstallDir = dialog.FolderName,
                Source = GameSource.Manual
            };
            _manualGames.Add(game);
            _allGames.Add(game);
            _allGames = _allGames.OrderBy(g => g.Name).ToList();
            RefreshGameList(GameSearchBox.Text);
            StatusText.Text = $"Added: {name}";
        }
    }

    // --- NPI ---

    private void NpiBuildProfile_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null)
        {
            StatusText.Text = "Select a game first.";
            return;
        }

        var window = new NpiProfileWindow(_selectedGame, _npi, _gpu) { Owner = this };
        window.ShowDialog();
    }

    private void NpiOpen_Click(object sender, RoutedEventArgs e)
    {
        if (!_npi.IsAvailable)
        {
            StatusText.Text = "NPI not found.";
            return;
        }

        _npi.LaunchNpi();
        StatusText.Text = "NPI opened.";
    }

    // --- DLL Swap ---

    private async void DllSwap_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null)
        {
            StatusText.Text = "Select a game first.";
            return;
        }

        if (DllTypeCombo.SelectedItem is not DllTypeComboItem selectedType)
        {
            StatusText.Text = "Select a DLL type first.";
            return;
        }

        if (DllVersionCombo.SelectedItem is not DllVersionComboItem selectedVersion)
        {
            StatusText.Text = "Select a version first.";
            return;
        }

        var preserveType = selectedType.Detected.Type;
        var progress = new Progress<string>(msg => StatusText.Text = msg);
        var result = await _dllSwap.SwapDllAsync(
            _selectedGame, selectedType.Detected, selectedVersion.Record, progress);

        StatusText.Text = result.Message;
        if (result.Success)
        {
            await ScanSelectedGameDllsAsync(preserveType);
            RefreshBackupsList();
        }
    }

    private async void DllRevert_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null)
        {
            StatusText.Text = "Select a game first.";
            return;
        }

        if (DllTypeCombo.SelectedItem is not DllTypeComboItem selectedType)
        {
            StatusText.Text = "Select a DLL type to revert.";
            return;
        }

        if (!_dllSwap.HasBackup(_selectedGame, selectedType.Detected))
        {
            StatusText.Text = "No backup exists for this DLL.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Restore original {DllTypeMap.Filenames[selectedType.Detected.Type]}?",
            "Confirm Revert", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        var preserveType = selectedType.Detected.Type;
        var result = _dllSwap.RevertDll(_selectedGame, selectedType.Detected);
        StatusText.Text = result.Message;
        if (result.Success)
            await ScanSelectedGameDllsAsync(preserveType);
    }

    // --- DLSSTweaks ---

    private void UpdateTweaksStatus()
    {
        if (_selectedGame is null)
        {
            TweaksStatus.Text = "No game selected.";
            return;
        }

        if (!_tweaks.IsAvailable)
        {
            TweaksStatus.Text = "DLSSTweaks source files not found.\nExpected:\n" +
                $"  {_settings.DlssTweaksDll}\n  {_settings.DlssTweaksIni}";
            return;
        }

        var status = _tweaks.GetStatus(_selectedGame);
        var tweaksTip = _gpu is not null ? $"\n\nTip for {_gpu.TierDisplay} GPU: " + _gpu.Tier switch
        {
            GpuTier.Ultra => "Force DLAA for max quality at native res. Your GPU can handle it.",
            GpuTier.High => "Try DLSS Quality or force DLAA if FPS is stable above your target.",
            GpuTier.Mid => "DLSS Balanced is a good starting point. Override to Quality if FPS allows.",
            GpuTier.Budget => "DLSS Performance mode recommended. Avoid DLAA — too heavy.",
            _ => ""
        } : "";
        TweaksStatus.Text = (status.IsDeployed
            ? $"Deployed: {status.Summary}\nINI: {status.IniPath}"
            : status.Summary) + tweaksTip;

        UpdateFgOverrideStatus();
    }

    private async void TweaksDeploy_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null)
        {
            StatusText.Text = "Select a game first.";
            return;
        }

        StatusText.Text = "Checking for anti-cheat...";
        var gameDir = _selectedGame.InstallDir;
        var antiCheat = await Task.Run(() => AntiCheatDetector.Detect(gameDir));
        if (antiCheat is not null)
        {
            StatusText.Text = $"BLOCKED: {antiCheat} detected. DLSSTweaks deploy aborted.";
            return;
        }

        var result = _tweaks.Deploy(_selectedGame);
        StatusText.Text = result;
        UpdateTweaksStatus();
    }

    private void TweaksEditConfig_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null)
        {
            StatusText.Text = "Select a game first.";
            return;
        }

        var status = _tweaks.GetStatus(_selectedGame);
        if (status.IniPath is null)
        {
            StatusText.Text = "No dlsstweaks.ini found. Deploy first.";
            return;
        }

        var editor = new TweaksConfigWindow(status.IniPath) { Owner = this };
        editor.ShowDialog();
        StatusText.Text = $"Config editor closed for {_selectedGame.Name}";
    }

    private void TweaksRemove_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null)
        {
            StatusText.Text = "Select a game first.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Remove DLSSTweaks from {_selectedGame.Name}?",
            "Confirm Remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        var result = _tweaks.Remove(_selectedGame);
        StatusText.Text = result;
        UpdateTweaksStatus();
    }

    private void PopulateFgVersionCombo()
    {
        if (_manifest is null)
        {
            FgVersionCombo.IsEnabled = false;
            return;
        }

        var records = _manifest.DlssG
            .Where(r => !r.IsDevFile)
            .OrderByDescending(r => r.VersionNumber)
            .Select((r, i) => new DllVersionComboItem(r, IsRecommended: i == 0))
            .ToList();

        FgVersionCombo.ItemsSource = records;
        FgVersionCombo.IsEnabled = records.Count > 0;
        if (records.Count > 0)
            FgVersionCombo.SelectedIndex = 0;
    }

    private void UpdateFgOverrideStatus()
    {
        if (_selectedGame is null)
        {
            FgOverrideStatus.Text = "No game selected.";
            FgApplyBtn.IsEnabled = false;
            FgClearBtn.IsEnabled = false;
            return;
        }

        var status = _tweaks.GetStatus(_selectedGame);
        if (!status.IsDeployed || status.IniPath is null)
        {
            FgOverrideStatus.Text = "Deploy DLSSTweaks first.";
            FgApplyBtn.IsEnabled = false;
            FgClearBtn.IsEnabled = false;
            return;
        }

        FgApplyBtn.IsEnabled = FgVersionCombo.IsEnabled;

        var overrides = TweaksConfigParser.LoadPathOverrides(status.IniPath);
        if (overrides.TryGetValue("nvngx_dlssg", out var activePath))
        {
            FgOverrideStatus.Text = $"Active: {activePath}";
            FgClearBtn.IsEnabled = true;
        }
        else
        {
            FgOverrideStatus.Text = "No override active.";
            FgClearBtn.IsEnabled = false;
        }
    }

    private async void FgApplyOverride_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;

        var status = _tweaks.GetStatus(_selectedGame);
        if (!status.IsDeployed || status.IniPath is null)
        {
            StatusText.Text = "Deploy DLSSTweaks first.";
            return;
        }

        if (FgVersionCombo.SelectedItem is not DllVersionComboItem selected)
        {
            StatusText.Text = "Select a Frame Gen version first.";
            return;
        }

        FgApplyBtn.IsEnabled = false;
        FgClearBtn.IsEnabled = false;

        try
        {
            var progress = new Progress<string>(msg => FgOverrideStatus.Text = msg);
            var cachedPath = await _fgOverride.EnsureCachedAsync(selected.Record, progress);
            TweaksConfigParser.WritePathOverride(status.IniPath, "nvngx_dlssg", cachedPath);
            StatusText.Text = $"FG override applied: v{selected.Record.Version}";
            UpdateFgOverrideStatus();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"FG override failed: {ex.Message}";
            FgOverrideStatus.Text = $"Error: {ex.Message}";
            FgApplyBtn.IsEnabled = FgVersionCombo.IsEnabled;
        }
    }

    private void FgClearOverride_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null) return;

        var status = _tweaks.GetStatus(_selectedGame);
        if (!status.IsDeployed || status.IniPath is null)
            return;

        TweaksConfigParser.WritePathOverride(status.IniPath, "nvngx_dlssg", null);
        StatusText.Text = "FG override cleared.";
        UpdateFgOverrideStatus();
    }

    // --- Game Refresh ---

    private void RefreshGames_Click(object sender, RoutedEventArgs e)
    {
        var previousNames = new HashSet<string>(_allGames.Select(g => g.Name));
        var steamGames = SteamDetectionService.DetectGames();
        var newNames = new HashSet<string>(steamGames.Select(g => g.Name));

        var added = steamGames.Where(g => !previousNames.Contains(g.Name)).ToList();
        var removed = _allGames
            .Where(g => g.Source == GameSource.Steam && !newNames.Contains(g.Name))
            .ToList();

        _allGames = steamGames.Concat(_manualGames).OrderBy(g => g.Name).ToList();

        if (_selectedGame is not null && !_allGames.Contains(_selectedGame))
        {
            _selectedGame = null;
            NpiCurrentProfile.Text = "No game selected";
            DllDetectedList.Text = "No game selected";
            TweaksStatus.Text = "No game selected";
            _detectedDlls = [];
            BackupsList.Items.Clear();
        }

        RefreshGameList(GameSearchBox.Text);

        var parts = new List<string>();
        if (added.Count > 0) parts.Add($"+{added.Count} new");
        if (removed.Count > 0) parts.Add($"-{removed.Count} removed");
        if (parts.Count == 0) parts.Add("no changes");

        StatusText.Text = $"Refreshed: {_allGames.Count} games ({string.Join(", ", parts)})";
    }

    // --- Backups ---

    private async void BackupRestore_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedGame is null)
        {
            StatusText.Text = "Select a game first.";
            return;
        }

        if (BackupsList.SelectedItem is not BackupEntry selected)
        {
            StatusText.Text = "Select a backup to restore.";
            return;
        }

        if (selected.Category == BackupCategory.Dll)
        {
            var target = _detectedDlls.FirstOrDefault(d =>
                DllTypeMap.Filenames[d.Type].Equals(selected.FileName, StringComparison.OrdinalIgnoreCase));

            if (target is null)
            {
                StatusText.Text = $"No matching DLL found in game directory for {selected.FileName}. It may have been removed.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Restore original {selected.FileName} to:\n{target.FullPath}",
                "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                File.Copy(selected.FilePath, target.FullPath, overwrite: true);
                StatusText.Text = $"Restored: {selected.FileName}";
                await ScanSelectedGameDllsAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Restore failed: {ex.Message}";
            }
        }
    }

    private void BackupDelete_Click(object sender, RoutedEventArgs e)
    {
        if (BackupsList.SelectedItem is not BackupEntry selected)
        {
            StatusText.Text = "Select a backup to delete.";
            return;
        }

        var confirm = MessageBox.Show(
            $"Delete backup?\n{selected.FileName}\n\nThis cannot be undone.",
            "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        var ok = _backups.DeleteBackup(selected);
        StatusText.Text = ok ? $"Deleted: {selected.FileName}" : "Delete failed.";
        RefreshBackupsList();
        UpdateDllBackupStatus();
    }

    private static Border CreateVendorWarning(string message)
    {
        return new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#302020")),
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F38BA8")),
            BorderThickness = new Thickness(1, 1, 1, 1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 16),
            Child = new TextBlock
            {
                Text = message,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F38BA8")),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            }
        };
    }
}

internal record DllTypeComboItem(DetectedDll Detected)
{
    public string Display => $"{DllTypeMap.Filenames[Detected.Type]}  (v{Detected.Version})";
}

internal record DllVersionComboItem(DllRecord Record, bool IsRecommended = false)
{
    public string Display
    {
        get
        {
            var sizeMb = Record.FileSize / (1024.0 * 1024.0);
            var label = string.IsNullOrEmpty(Record.AdditionalLabel) ? "" : $"  [{Record.AdditionalLabel}]";
            var rec = IsRecommended ? "  [Recommended]" : "";
            return $"v{Record.Version}  ({sizeMb:F1} MB){label}{rec}";
        }
    }
}
