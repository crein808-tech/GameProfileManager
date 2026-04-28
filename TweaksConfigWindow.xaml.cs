using System.Windows;
using System.Windows.Controls;
using GameProfileManager.Models;
using GameProfileManager.Services;

namespace GameProfileManager;

public partial class TweaksConfigWindow : Window
{
    private readonly string _iniPath;
    private readonly List<TweaksSetting> _settings;
    private readonly Dictionary<TweaksSetting, FrameworkElement> _controls = new();
    private readonly Dictionary<string, string> _pathOverrides;

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
