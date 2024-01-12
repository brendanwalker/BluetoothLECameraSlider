using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CameraSlider.UI
{
	/// <summary>
	/// Interaction logic for EditPresetWindow.xaml
	/// </summary>
	public partial class EditPresetWindow : Window
	{
		private CameraSettingsSection _cameraSettings;
		private List<PresetSettings> _presets= new List<PresetSettings>();
		private int _editPresetIndex= -1;

		private PresetSettings _preset;

		public EditPresetWindow()
		{
			InitializeComponent();
		}

		public EditPresetWindow(
			CameraSettingsSection cameraSettings, 
			List<PresetSettings> presets, 
			int editPresetIndex)
		{
			InitializeComponent();

			_cameraSettings= cameraSettings;
			_presets = presets;
			_editPresetIndex = editPresetIndex;

			if (editPresetIndex >= 0 && editPresetIndex < presets.Count)
			{
				_preset= new PresetSettings(presets[_editPresetIndex]);
			}
			else
			{
				_preset= new PresetSettings();
				_preset.PresetName= "Preset_"+presets.Count;
			}

			// Copy the preset settings to the UI
			PresetNameTxt.Text = _preset.PresetName;
			SlidePosTxt.Text = _preset.SlidePosition.ToString("0.00");
			PanPosTxt.Text = _preset.PanPosition.ToString("0.00");
			TiltPosTxt.Text = _preset.TiltPosition.ToString("0.00");
			OBSSceneTxt.Text = _preset.ObsScene;
			ChatCommandTxt.Text = _preset.ChatTrigger.TriggerName;
			IsChatModOnlyChk.IsChecked = _preset.ChatTrigger.IsModOnly;
			IsChatTriggeredChk.IsChecked = _preset.ChatTrigger.IsActive;
			RedeemTxt.Text = _preset.RedeemTrigger.TriggerName;
			IsRedeemModOnlyChk.IsChecked = _preset.RedeemTrigger.IsModOnly;
			IsRedeemActiveChk.IsChecked = _preset.RedeemTrigger.IsActive;
		}

		private void PresetNameTxt_TextChanged(object sender, TextChangedEventArgs e)
		{
			_preset.PresetName = PresetNameTxt.Text;
        }

		private void SlidePosTxt_TextChanged(object sender, TextChangedEventArgs e)
		{
			_preset.SlidePosition = float.Parse(SlidePosTxt.Text);
		}

		private void PanPosTxt_TextChanged(object sender, TextChangedEventArgs e)
		{
			_preset.PanPosition = int.Parse(PanPosTxt.Text);
		}

		private void TiltPosTxt_TextChanged(object sender, TextChangedEventArgs e)
		{
			_preset.TiltPosition = int.Parse(TiltPosTxt.Text);
		}

		private void UpdatePosBtn_Click(object sender, RoutedEventArgs e)
		{
			_preset.SlidePosition= _cameraSettings.SlidePos;
			_preset.PanPosition= _cameraSettings.PanPos;
			_preset.TiltPosition= _cameraSettings.TiltPos;

			SlidePosTxt.Text= _preset.SlidePosition.ToString("0.00");
			PanPosTxt.Text= _preset.PanPosition.ToString("0.00");
			TiltPosTxt.Text= _preset.TiltPosition.ToString("0.00");
		}

		private void ObsSceneTxt_TextChanged(object sender, TextChangedEventArgs e)
		{
			_preset.ObsScene = OBSSceneTxt.Text;
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
			if (_editPresetIndex >= 0 && _editPresetIndex < _presets.Count)
			{
				_presets[_editPresetIndex] = _preset;
			}
			else
			{
				_presets.Add(_preset);
			}

			this.Close();
		}

		private void CancelChangesBtn_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}
	}
}
