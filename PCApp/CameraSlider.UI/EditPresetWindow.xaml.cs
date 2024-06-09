using System.Windows;
using System.Windows.Controls;

namespace CameraSlider.UI
{
  /// <summary>
  /// Interaction logic for EditPresetWindow.xaml
  /// </summary>
  public partial class EditPresetWindow : Window
  {
    ConfigState _configState = null;
    private int _editPresetIndex = -1;

    private PresetSettings _preset;

    public EditPresetWindow()
    {
      InitializeComponent();
    }

    public EditPresetWindow(
      ConfigState configState,
      int editPresetIndex)
    {
      InitializeComponent();

      _configState = configState;
      _editPresetIndex = editPresetIndex;

      if (editPresetIndex >= 0 && editPresetIndex < _configState._presets.Count)
      {
        _preset = new PresetSettings(_configState._presets[_editPresetIndex]);
      }
      else
      {
        _preset = new PresetSettings();
        _preset.PresetName = "Preset_" + _configState._presets.Count;
        _preset.PanPosition = _configState._cameraSettingsConfig.PanPos;
        _preset.SlidePosition = _configState._cameraSettingsConfig.SlidePos;
        _preset.TiltPosition = _configState._cameraSettingsConfig.TiltPos;
      }

      // Copy the preset settings to the UI
      PresetNameTxt.Text = _preset.PresetName;
      SlidePosTxt.Text = _preset.SlidePosition.ToString("0.00");
      PanPosTxt.Text = _preset.PanPosition.ToString("0.00");
      TiltPosTxt.Text = _preset.TiltPosition.ToString("0.00");
    }

    private void PresetNameTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (_preset != null)
        _preset.PresetName = PresetNameTxt.Text;
    }

    private void SlidePosTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      float pos;
      if (_preset != null && float.TryParse(SlidePosTxt.Text, out pos))
      {
        _preset.SlidePosition = pos;
      }
    }

    private void PanPosTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      float pos;
      if (_preset != null && float.TryParse(PanPosTxt.Text, out pos))
      {
        _preset.PanPosition = pos;
      }
    }

    private void TiltPosTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      float pos;
      if (_preset != null && float.TryParse(TiltPosTxt.Text, out pos))
      {
        _preset.TiltPosition = pos;
      }
    }

    private void UpdatePosBtn_Click(object sender, RoutedEventArgs e)
    {
      _preset.SlidePosition = _configState._cameraSettingsConfig.SlidePos;
      _preset.PanPosition = _configState._cameraSettingsConfig.PanPos;
      _preset.TiltPosition = _configState._cameraSettingsConfig.TiltPos;

      SlidePosTxt.Text = _preset.SlidePosition.ToString("0.00");
      PanPosTxt.Text = _preset.PanPosition.ToString("0.00");
      TiltPosTxt.Text = _preset.TiltPosition.ToString("0.00");
    }

    private void SavePresetBtn_Click(object sender, RoutedEventArgs e)
    {
      if (_editPresetIndex >= 0 && _editPresetIndex < _configState._presets.Count)
      {
        _configState._presets[_editPresetIndex] = _preset;
      }
      else
      {
        _configState._presets.Add(_preset);
      }
      _configState._arePresetsDirty = true;
      _configState._areConfigSettingsDirty = true;

      this.Close();
    }

    private void CancelChangesBtn_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }
  }
}
