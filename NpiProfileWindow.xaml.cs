using System.IO;
using System.Windows;
using System.Windows.Controls;
using GameProfileManager.Models;
using GameProfileManager.Services;

namespace GameProfileManager;

public partial class NpiProfileWindow : Window
{
    private readonly GameEntry _game;
    private readonly NpiService _npi;
    private readonly GpuInfo? _gpu;
    private readonly List<NpiSettingDef> _settings;
    private readonly Dictionary<NpiSettingDef, ComboBox> _controls = new();

    public NpiProfileWindow(GameEntry game, NpiService npi, GpuInfo? gpu)
    {
        _game = game;
        _npi = npi;
        _gpu = gpu;
        _settings = NpiProfileGenerator.GetSettingsTemplate();

        InitializeComponent();

        GameNameText.Text = game.Name;
        ProfileNameBox.Text = game.Name;

        BuildPresetBar();
        BuildUI();
    }

    private void BuildPresetBar()
    {
        var tier = _gpu?.Tier ?? GpuTier.Mid;
        var presets = NpiProfileGenerator.GetPresets(tier);

        if (_gpu is not null)
        {
            PresetPanel.Children.Add(new TextBlock
            {
                Text = $"Detected: {_gpu}",
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#6C7086")),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var preset in presets)
        {
            var btn = new Button
            {
                Content = preset.Name,
                ToolTip = preset.Description,
                Margin = new Thickness(0, 0, 8, 0),
                Tag = preset.Name
            };
            btn.Click += PresetButton_Click;
            buttonPanel.Children.Add(btn);
        }

        var resetBtn = new Button
        {
            Content = "Reset All",
            Margin = new Thickness(0, 0, 0, 0),
            Tag = "__reset__"
        };
        resetBtn.Click += PresetButton_Click;
        buttonPanel.Children.Add(resetBtn);

        PresetPanel.Children.Add(buttonPanel);
    }

    private void PresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not string tag) return;

        var tier = _gpu?.Tier ?? GpuTier.Mid;

        if (tag == "__reset__")
        {
            foreach (var setting in _settings)
                setting.SelectedHex = setting.DefaultHex;
        }
        else
        {
            NpiProfileGenerator.ApplyPreset(_settings, tier, tag);
        }

        foreach (var (setting, combo) in _controls)
        {
            var idx = Array.FindIndex(setting.Options, o => o.HexValue == setting.SelectedHex);
            combo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        var changed = _settings.Count(s => s.SelectedHex != s.DefaultHex);
        EditorStatus.Text = tag == "__reset__"
            ? "All settings reset to defaults."
            : $"Applied '{tag}' preset — {changed} setting(s) changed.";
    }

    private void BuildUI()
    {
        var currentCategory = "";

        foreach (var setting in _settings)
        {
            if (setting.Category != currentCategory)
            {
                currentCategory = setting.Category;
                SettingsPanel.Children.Add(new TextBlock
                {
                    Text = currentCategory,
                    Style = (Style)FindResource("SectionHeader")
                });
                SettingsPanel.Children.Add(new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#313244")),
                    Height = 1,
                    Margin = new Thickness(0, 0, 0, 8)
                });
            }

            var container = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#181825")),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 0, 0, 6)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = setting.Name,
                Style = (Style)FindResource("SettingLabel")
            });
            stack.Children.Add(new TextBlock
            {
                Text = setting.Description,
                Style = (Style)FindResource("SettingDesc")
            });

            var combo = new ComboBox { MinWidth = 250, Margin = new Thickness(0, 2, 0, 0) };
            foreach (var opt in setting.Options)
                combo.Items.Add(opt.Label);

            var defaultIdx = Array.FindIndex(setting.Options, o => o.HexValue == setting.DefaultHex);
            combo.SelectedIndex = defaultIdx >= 0 ? defaultIdx : 0;

            _controls[setting] = combo;
            stack.Children.Add(combo);
            container.Child = stack;
            SettingsPanel.Children.Add(container);
        }
    }

    private void CollectValues()
    {
        foreach (var (setting, combo) in _controls)
        {
            var idx = combo.SelectedIndex;
            setting.SelectedHex = idx >= 0 && idx < setting.Options.Length
                ? setting.Options[idx].HexValue
                : setting.DefaultHex;
        }
    }

    private string? GetProfileName()
    {
        var name = ProfileNameBox.Text?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            EditorStatus.Text = "Enter a profile name first.";
            return null;
        }
        return name;
    }

    private string? SaveProfile()
    {
        var profileName = GetProfileName();
        if (profileName is null) return null;

        CollectValues();
        var changed = _settings.Count(s => s.SelectedHex != s.DefaultHex);

        if (changed == 0)
        {
            EditorStatus.Text = "No settings changed from defaults.";
            return null;
        }

        var nipPath = Path.Combine(_npi.ProfilesDir, $"{SanitizeFileName(_game.Name)}_profile.nip");

        try
        {
            NpiProfileGenerator.GenerateNip(nipPath, profileName, _settings);
            EditorStatus.Text = $"Saved {changed} setting(s): {Path.GetFileName(nipPath)}";
            return nipPath;
        }
        catch (Exception ex)
        {
            EditorStatus.Text = $"Save failed: {ex.Message}";
            return null;
        }
    }

    private void SaveNip_Click(object sender, RoutedEventArgs e)
    {
        var profileName = GetProfileName();
        if (profileName is null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save NVIDIA Profile",
            Filter = "NPI Profiles (*.nip)|*.nip",
            DefaultExt = ".nip",
            InitialDirectory = _npi.ProfilesDir,
            FileName = $"{SanitizeFileName(_game.Name)}_profile.nip"
        };

        if (dialog.ShowDialog() != true) return;

        CollectValues();
        var changed = _settings.Count(s => s.SelectedHex != s.DefaultHex);

        if (changed == 0)
        {
            EditorStatus.Text = "No settings changed from defaults. Nothing to save.";
            return;
        }

        try
        {
            NpiProfileGenerator.GenerateNip(dialog.FileName, profileName, _settings);
            EditorStatus.Text = $"Saved: {Path.GetFileName(dialog.FileName)} ({changed} setting(s))";
        }
        catch (Exception ex)
        {
            EditorStatus.Text = $"Save failed: {ex.Message}";
        }
    }

    private void ApplyNow_Click(object sender, RoutedEventArgs e)
    {
        var nipPath = SaveProfile();
        if (nipPath is null) return;

        Clipboard.SetText(nipPath);
        _npi.LaunchNpi();
        EditorStatus.Text += " | NPI opened. Import > Ctrl+V > Open > Apply Changes.";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid));
    }
}
