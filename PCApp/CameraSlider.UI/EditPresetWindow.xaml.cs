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
      OBSSceneTxt.Text = _preset.ObsScene;
      HoldDurationTxt.Text = _preset.HoldDuration.ToString("0.00");
      ChatCommandTxt.Text = _preset.ChatTrigger.TriggerName;
      IsChatModOnlyChk.IsChecked = _preset.ChatTrigger.IsModOnly;
      IsChatTriggeredChk.IsChecked = _preset.ChatTrigger.IsActive;
      RedeemTxt.Text = _preset.RedeemTrigger.TriggerName;
      IsRedeemModOnlyChk.IsChecked = _preset.RedeemTrigger.IsModOnly;
      IsRedeemActiveChk.IsChecked = _preset.RedeemTrigger.IsActive;
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

    private void ObsSceneTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      _preset.ObsScene = OBSSceneTxt.Text;
    }

    private void HoldDurationTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      float holdDuration= 0f;
      if (float.TryParse(HoldDurationTxt.Text, out holdDuration))
      {
        _preset.HoldDuration= holdDuration;
      }
    }

    private void ChatCommandTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      _preset.ChatTrigger.TriggerName = ChatCommandTxt.Text;
    }

    private void IsChatModOnlyChk_Checked(object sender, RoutedEventArgs e)
    {
      _preset.ChatTrigger.IsModOnly = IsChatModOnlyChk.IsChecked.Value;
    }

    private void IsChatTriggeredChk_Checked(object sender, RoutedEventArgs e)
    {
      _preset.ChatTrigger.IsActive = IsChatTriggeredChk.IsChecked.Value;
    }

    private void RedeemTxt_TextChanged(object sender, TextChangedEventArgs e)
    {
      _preset.RedeemTrigger.TriggerName = RedeemTxt.Text;
    }

    private void IsRedeemModOnlyChk_Checked(object sender, RoutedEventArgs e)
    {
      _preset.RedeemTrigger.IsModOnly = IsRedeemModOnlyChk.IsChecked.Value;
    }

    private void IsRedeemActiveChk_Checked(object sender, RoutedEventArgs e)
    {
      _preset.RedeemTrigger.IsActive = IsRedeemActiveChk.IsChecked.Value;
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
