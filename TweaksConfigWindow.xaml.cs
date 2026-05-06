using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameProfileManager.Models;
using GameProfileManager.Services;

namespace GameProfileManager;

public partial class TweaksConfigWindow : Window
{
    private readonly string _iniPath;
    private readonly List<TweaksSetting> _settings;
    private readonly Dictionary<TweaksSetting, FrameworkElement> _controls = new();
    private readonly Dictionary<string, string> _pathOverrides;
    private readonly List<(string DllKey, TextBox PathBox)> _pathOverrideRows = [];
    private StackPanel _pathOverrideRowsPanel = new();
    private ComboBox _addDllKeyCombo = new();

    public TweaksConfigWindow(string iniPath)
    {
        _iniPath = iniPath;
        _settings = TweaksConfigParser.GetSettingsTemplate();
        TweaksConfigParser.LoadFromIni(iniPath, _settings);
        _pathOverrides = TweaksConfigParser.LoadPathOverrides(iniPath);

        InitializeComponent();
        BuildUI();
    }

    private void BuildUI()
    {
        var currentSection = "";

        foreach (var setting in _settings)
        {
            if (setting.Section != currentSection)
            {
                currentSection = setting.Section;
                var header = new TextBlock
                {
                    Text = currentSection,
                    Style = (Style)FindResource("SectionHeader")
                };
                SettingsPanel.Children.Add(header);

                var separator = new Border
                {
                    Background = new System.Windows.Media.SolidColorBrush(
                        (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#313244")),
                    Height = 1,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                SettingsPanel.Children.Add(separator);
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

            var label = new TextBlock
            {
                Text = setting.Label,
                Style = (Style)FindResource("SettingLabel")
            };
            stack.Children.Add(label);

            var desc = new TextBlock
            {
                Text = setting.Description,
                Style = (Style)FindResource("SettingDesc")
            };
            stack.Children.Add(desc);

            FrameworkElement control;

            switch (setting.Type)
            {
                case TweaksSettingType.Bool:
                    var cb = new CheckBox
                    {
                        IsChecked = setting.Value.Equals("true", StringComparison.OrdinalIgnoreCase),
                        Foreground = new System.Windows.Media.SolidColorBrush(
                            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#CDD6F4")),
                        Content = "Enabled",
                        Margin = new Thickness(0, 2, 0, 0)
                    };
                    control = cb;
                    break;

                case TweaksSettingType.Dropdown:
                    var combo = new ComboBox { Margin = new Thickness(0, 2, 0, 0), MinWidth = 200 };
                    foreach (var opt in setting.Options)
                        combo.Items.Add(opt);

                    var match = setting.Options.FirstOrDefault(o =>
                        o.Equals(setting.Value, StringComparison.OrdinalIgnoreCase) ||
                        o.StartsWith(setting.Value + " ", StringComparison.OrdinalIgnoreCase) ||
                        o.StartsWith(setting.Value + "(", StringComparison.OrdinalIgnoreCase));

                    combo.SelectedItem = match ?? setting.Value;
                    if (combo.SelectedItem is null && combo.Items.Count > 0)
                        combo.SelectedIndex = 0;
                    control = combo;
                    break;

                default:
                    var tb = new TextBox
                    {
                        Text = setting.Value,
                        Margin = new Thickness(0, 2, 0, 0),
                        MinWidth = 200
                    };
                    control = tb;
                    break;
            }

            _controls[setting] = control;
            stack.Children.Add(control);
            container.Child = stack;
            SettingsPanel.Children.Add(container);
        }

        BuildPathOverridesUI();
    }

    private static SolidColorBrush Brush(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

    private void BuildPathOverridesUI()
    {
        SettingsPanel.Children.Add(new TextBlock
        {
            Text = "DLLPathOverrides",
            Style = (Style)FindResource("SectionHeader")
        });
        SettingsPanel.Children.Add(new Border
        {
            Background = Brush("#313244"),
            Height = 1,
            Margin = new Thickness(0, 0, 0, 8)
        });

        _pathOverrideRowsPanel = new StackPanel();
        SettingsPanel.Children.Add(_pathOverrideRowsPanel);

        foreach (var (key, value) in _pathOverrides)
            AddPathOverrideRow(key, value);

        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };

        _addDllKeyCombo = new ComboBox { MinWidth = 200, Margin = new Thickness(0, 0, 8, 0) };
        foreach (var fn in DllTypeMap.Filenames.Values)
            _addDllKeyCombo.Items.Add(Path.GetFileNameWithoutExtension(fn));
        if (_addDllKeyCombo.Items.Count > 0)
            _addDllKeyCombo.SelectedIndex = 0;

        var addBtn = new Button
        {
            Content = "Add Override",
            Style = (Style)FindResource("AccentButtonStyle"),
            Padding = new Thickness(12, 4, 12, 4)
        };
        addBtn.Click += (_, _) =>
        {
            var key = _addDllKeyCombo.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(key))
                AddPathOverrideRow(key, "");
        };

        addRow.Children.Add(_addDllKeyCombo);
        addRow.Children.Add(addBtn);
        SettingsPanel.Children.Add(addRow);
    }

    private void AddPathOverrideRow(string dllKey, string currentValue)
    {
        var row = new Border
        {
            Background = Brush("#181825"),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var label = new TextBlock
        {
            Text = dllKey,
            Foreground = Brush("#CDD6F4"),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        Grid.SetColumn(label, 0);

        var pathBox = new TextBox
        {
            Text = currentValue,
            Margin = new Thickness(0, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(pathBox, 1);

        var browseBtn = new Button
        {
            Content = "Browse",
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(0, 0, 4, 0)
        };
        Grid.SetColumn(browseBtn, 2);
        browseBtn.Click += (_, _) =>
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = $"Select DLL for {dllKey}",
                Filter = "DLL (*.dll)|*.dll|All files (*.*)|*.*",
                FileName = pathBox.Text
            };
            if (dlg.ShowDialog() == true)
                pathBox.Text = dlg.FileName;
        };

        var tuple = (dllKey, pathBox);
        _pathOverrideRows.Add(tuple);

        var clearBtn = new Button
        {
            Content = "✕",
            Padding = new Thickness(6, 2, 6, 2),
            Foreground = Brush("#F38BA8")
        };
        Grid.SetColumn(clearBtn, 3);
        clearBtn.Click += (_, _) =>
        {
            _pathOverrideRows.Remove(tuple);
            _pathOverrideRowsPanel.Children.Remove(row);
        };

        grid.Children.Add(label);
        grid.Children.Add(pathBox);
        grid.Children.Add(browseBtn);
        grid.Children.Add(clearBtn);
        row.Child = grid;
        _pathOverrideRowsPanel.Children.Add(row);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        foreach (var (setting, control) in _controls)
        {
            setting.Value = control switch
            {
                CheckBox cb => cb.IsChecked == true ? "true" : "false",
                ComboBox combo => combo.SelectedItem?.ToString() ?? setting.DefaultValue,
                TextBox tb => tb.Text,
                _ => setting.DefaultValue
            };
        }

        _pathOverrides.Clear();
        foreach (var (key, box) in _pathOverrideRows)
        {
            var val = box.Text.Trim();
            if (!string.IsNullOrEmpty(val))
                _pathOverrides[key] = val;
        }

        try
        {
            TweaksConfigParser.SaveToIni(_iniPath, _settings, _pathOverrides);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            EditorStatus.Text = $"Save failed: {ex.Message}";
        }
    }
}
