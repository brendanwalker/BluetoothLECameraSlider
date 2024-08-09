using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using CameraSlider.Bluetooth.Events;
using System.ComponentModel;
using CameraSlider.Bluetooth;
using System.Threading;
using System.Text.RegularExpressions;
using CameraSlider.UI.Config;

namespace CameraSlider.UI
{

	public partial class MainWindow : Window
	{
		[Flags]
		public enum UIControlDisableFlags
		{
			None = 0,
			DeviceDisconnected = 1,
			Calibrating = 1 << 1,
		}

		private UIControlDisableFlags _uiDisableBitmask = UIControlDisableFlags.None;

		// Config
		private ConfigState _configState = new ConfigState();

		// Camera Slider
		CancellationTokenSource _deviceWatchdogCancelSignaler = new CancellationTokenSource();
		private string _deviceName = "Camera Slider";
		private CameraSliderDevice _cameraSliderDevice;
		private bool _deviceCalibrationRunning = false;
		private static int _deviceKeepAliveDelay = 10;
		private int _deviceKeepAliveCountdown = _deviceKeepAliveDelay;
		private bool _suppressUIUpdatesToDevice = false;

		// Preset state
		CancellationTokenSource _presetCancelSignaler = null;
		private float _presetTargetSlidePosition = 0.0f;
		private float _presetTargetPanPosition = 0.0f;
		private float _presetTargetTiltPosition = 0.0f;
		private bool _hasPendingPresetTarget = false;

		// Slider state
		private bool _isSliderPosDragging = false;
		private bool _isPanPosDragging = false;
		private bool _isTiltPosDragging = false;
		private bool _isSliderSpeedDragging = false;
		private bool _isPanSpeedDragging = false;
		private bool _isTiltSpeedDragging = false;
		private bool _isSliderAccelDragging = false;
		private bool _isPanAccelDragging = false;
		private bool _isTiltAccelDragging = false;

		public MainWindow()
		{
			InitializeComponent();

			// Load the config settings first
			_configState.LoadConfig();

			// Register to Bluetooth LE Camera Slider Device Manager
			_cameraSliderDevice = new CameraSliderDevice();
			_cameraSliderDevice.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;
			_cameraSliderDevice.CameraSliderEventHandler += CameraSliderEventReceived;

			// Setup the UI
			SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, true);
			ApplyConfigToUI();

			// Kick off the watchdog workers
			_ = StartDeviceWatchdogWorker(_deviceWatchdogCancelSignaler.Token);
		}

		protected async override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			// Stop any running async tasks
			_deviceWatchdogCancelSignaler.Cancel();
			if (_presetCancelSignaler != null)
			{
				_presetCancelSignaler.Cancel();
			}

			// Disconnect from everything
			if (_cameraSliderDevice.IsConnected)
			{
				await _cameraSliderDevice.DisconnectAsync();
			}
		}

		// Camera Slider Functions
		private async void SetCameraStatusLabel(string status)
		{
			await RunOnUiThread(() =>
			{
				CameraTxtStatus.Content = status;
			});
		}

		private void CameraSliderEventReceived(object sender, CameraSliderEventArgs arg)
		{
			EmitLog("Received slider event received: " + arg.Message);

			switch (arg.Message)
			{
			case "move_complete":
			{
				_hasPendingPresetTarget = false;
			}
			break;
			case "calibration_started":
			{
				_deviceCalibrationRunning = true;
				SetUIControlsDisableFlag(UIControlDisableFlags.Calibrating, true);
				SetCameraStatusLabel("Calibrating...");
			}
			break;
			case "calibration_completed":
			{
				_deviceCalibrationRunning = false;
				SetUIControlsDisableFlag(UIControlDisableFlags.Calibrating, false);
				SetCameraStatusLabel("Connected");
			}
			break;
			case "calibration_failed":
			{
				_deviceCalibrationRunning = false;
				SetUIControlsDisableFlag(UIControlDisableFlags.Calibrating, false);
				SetCameraStatusLabel("Calibration Failed!");
			}
			break;
			}
		}

		private async void OnDeviceConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
		{
			bool connected = args.IsConnected;
			EmitLog("CameraSlider connection status is: " + connected);

			if (connected)
			{
				SetCameraStatusLabel("Connected");
				SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, false);

				await _cameraSliderDevice.SetSlidePosition(_configState._cameraSettingsConfig.SlidePos);
				await _cameraSliderDevice.SetPanPosition(_configState._cameraSettingsConfig.PanPos);
				await _cameraSliderDevice.SetTiltPosition(_configState._cameraSettingsConfig.TiltPos);

				await _cameraSliderDevice.SetSlideSpeed(_configState._cameraSettingsConfig.SlideSpeed);
				await _cameraSliderDevice.SetPanSpeed(_configState._cameraSettingsConfig.PanSpeed);
				await _cameraSliderDevice.SetTiltSpeed(_configState._cameraSettingsConfig.TiltSpeed);

				await _cameraSliderDevice.SetSlideAcceleration(_configState._cameraSettingsConfig.SlideAccel);
				await _cameraSliderDevice.SetPanAcceleration(_configState._cameraSettingsConfig.PanAccel);
				await _cameraSliderDevice.SetTiltAcceleration(_configState._cameraSettingsConfig.TiltAccel);

				_suppressUIUpdatesToDevice = true;

				await RunOnUiThread(() =>
				{
					SlidePosSlider.Value = _configState._cameraSettingsConfig.SlidePos;
					PanPosSlider.Value = _configState._cameraSettingsConfig.PanPos;
					TiltPosSlider.Value = _configState._cameraSettingsConfig.TiltPos;

					SlideSpeedSlider.Value = _configState._cameraSettingsConfig.SlideSpeed;
					PanSpeedSlider.Value = _configState._cameraSettingsConfig.PanSpeed;
					TiltSpeedSlider.Value = _configState._cameraSettingsConfig.TiltSpeed;

					SlideAccelSlider.Value = _configState._cameraSettingsConfig.SlideAccel;
					PanAccelSlider.Value = _configState._cameraSettingsConfig.PanAccel;
					TiltAccelSlider.Value = _configState._cameraSettingsConfig.TiltAccel;
				});

				_suppressUIUpdatesToDevice = false;
			}
			else
			{
				SetCameraStatusLabel("Searching...");
				SetActivePresetStatusLabel("");
				SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, true);
			}
		}

		private async Task StartDeviceWatchdogWorker(CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					// Ping a connected device regularly to keep it awake
					if (_cameraSliderDevice.IsConnected)
					{
						_deviceKeepAliveCountdown--;
						if (_deviceKeepAliveCountdown <= 0)
						{
							await _cameraSliderDevice.WakeUp();
							_deviceKeepAliveCountdown = _deviceKeepAliveDelay;
						}
					}
					// Attempt to kick off an async device reconnection
					else if (!_cameraSliderDevice.IsConnected)
					{
						try
						{
							await _cameraSliderDevice.ConnectAsync(_deviceName);
						}
						catch (Exception ex)
						{
							EmitLog("Device connect error: " + ex.Message);
						}

						// Clear the control UI disable if the device is connected
						if (_cameraSliderDevice.IsConnected)
						{
							SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, false);
						}
					}

					if (_configState._areConfigSettingsDirty)
					{
						_configState.SaveConfig();
					}

					await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				// Handle the cancellation
				Console.WriteLine("Shut down watch dog worker");
			}
		}

		private void ActivatePreset(PresetSettings preset)
		{
			// Cancel any previous preset status polling
			if (_presetCancelSignaler != null)
			{
				_presetCancelSignaler.Cancel();
			}

			// Start an async worker to move the camera to the preset position
			_presetCancelSignaler = new CancellationTokenSource();
			_ = StartPresetWorkerAsync(preset, _presetCancelSignaler.Token);
		}

		private async Task StartPresetWorkerAsync(PresetSettings preset, CancellationToken cancellationToken)
		{
			// Store our preset target
			_presetTargetSlidePosition = preset.SlidePosition;
			_presetTargetPanPosition = preset.PanPosition;
			_presetTargetTiltPosition = preset.TiltPosition;

			// Update the UI to show the preset target
			SetActivePresetStatusLabel("Moving To " + preset.PresetName);
			_hasPendingPresetTarget = true;
			await _cameraSliderDevice.SetSlidePosition(_presetTargetSlidePosition);
			await _cameraSliderDevice.SetPanPosition(_presetTargetPanPosition);
			await _cameraSliderDevice.SetTiltPosition(_presetTargetTiltPosition);

			await RunOnUiThread(() =>
			{
				SlidePosSlider.Value = preset.SlidePosition;
				PanPosSlider.Value = preset.PanPosition;
				TiltPosSlider.Value = preset.TiltPosition;
			});

			// Wait for the camera to reach the target position
			try
			{
				const float maxWaitDurationSeconds = 10f;
				float activeDurationSeconds = 0f;

				while (_hasPendingPresetTarget && activeDurationSeconds < maxWaitDurationSeconds)
				{
					var waitDuration = TimeSpan.FromMilliseconds(100);
					activeDurationSeconds += ((float)waitDuration.TotalSeconds);

					await Task.Delay(waitDuration, cancellationToken);
				}

				_hasPendingPresetTarget = false;
			}
			catch (OperationCanceledException)
			{
				// Handle the cancellation
				Console.WriteLine("Preset was cancelled.");
			}

			SetActivePresetStatusLabel("");
		}

		private async void SetActivePresetStatusLabel(string status)
		{
			await RunOnUiThread(() =>
			{
				ActivePresetTxtStatus.Content = status;
			});
		}

		// UI Functions
		protected void ApplyConfigToUI()
		{
			SlidePosSlider.Value = _configState._cameraSettingsConfig.SlidePos;
			SlideSpeedSlider.Value = _configState._cameraSettingsConfig.SlideSpeed;
			SlideAccelSlider.Value = _configState._cameraSettingsConfig.SlideAccel;
			PanPosSlider.Value = _configState._cameraSettingsConfig.PanPos;
			PanSpeedSlider.Value = _configState._cameraSettingsConfig.PanSpeed;
			PanAccelSlider.Value = _configState._cameraSettingsConfig.PanAccel;
			TiltPosSlider.Value = _configState._cameraSettingsConfig.TiltPos;
			TiltSpeedSlider.Value = _configState._cameraSettingsConfig.TiltSpeed;
			TiltAccelSlider.Value = _configState._cameraSettingsConfig.TiltAccel;

			SlidePosStatus.Content = _configState._cameraSettingsConfig.SlidePos.ToString("0.00");
			SlideSpeedStatus.Content = _configState._cameraSettingsConfig.SlideSpeed.ToString("0.00");
			SlideAccelStatus.Content = _configState._cameraSettingsConfig.SlideAccel.ToString("0.00");
			PanPosStatus.Content = _configState._cameraSettingsConfig.PanPos.ToString("0.00");
			PanSpeedStatus.Content = _configState._cameraSettingsConfig.PanSpeed.ToString("0.00");
			PanAccelStatus.Content = _configState._cameraSettingsConfig.PanAccel.ToString("0.00");
			TiltPosStatus.Content = _configState._cameraSettingsConfig.TiltPos.ToString("0.00");
			TiltSpeedStatus.Content = _configState._cameraSettingsConfig.TiltSpeed.ToString("0.00");
			TiltAccelStatus.Content = _configState._cameraSettingsConfig.TiltAccel.ToString("0.00");

			RebuildPresetComboBox();
		}

		private void SetUIControlsDisableFlag(UIControlDisableFlags flag, bool bSetFlag)
		{
			UIControlDisableFlags newFlags = _uiDisableBitmask;

			if (bSetFlag)
				newFlags |= flag;
			else
				newFlags &= ~flag;

			SetUIDisableBitmask(newFlags);
		}

		private async void SetUIDisableBitmask(UIControlDisableFlags newFlags)
		{
			if (_uiDisableBitmask != newFlags)
			{
				bool bEnabled = newFlags == UIControlDisableFlags.None;

				await RunOnUiThread(() =>
				{
					SlidePosSlider.IsEnabled = bEnabled;
					SlideSpeedSlider.IsEnabled = bEnabled;
					SlideAccelSlider.IsEnabled = bEnabled;

					PanPosSlider.IsEnabled = bEnabled;
					PanSpeedSlider.IsEnabled = bEnabled;
					PanAccelSlider.IsEnabled = bEnabled;

					BtnCalibrate.IsEnabled = bEnabled;
					BtnHalt.IsEnabled = bEnabled;

					BtnAddPreset.IsEnabled = bEnabled;
					BtnEditPreset.IsEnabled = bEnabled;
					BtnDeletePreset.IsEnabled = bEnabled;
					BtnGotoPreset.IsEnabled = bEnabled;
				});

				_uiDisableBitmask = newFlags;
			}
		}

		private async Task RunOnUiThread(Action a)
		{
			await this.Dispatcher.InvokeAsync(() =>
			{
				a();
			});
		}

		private void NumericalInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private async void SlidePos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.SlidePos = (float)SlidePosSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isSliderPosDragging)
				await _cameraSliderDevice.SetSlidePosition(_configState._cameraSettingsConfig.SlidePos);
			SlidePosStatus.Content = _configState._cameraSettingsConfig.SlidePos.ToString("0.00");
		}

		private async void PanPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.PanPos = (float)PanPosSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isPanPosDragging)
				await _cameraSliderDevice.SetPanPosition(_configState._cameraSettingsConfig.PanPos);
			PanPosStatus.Content = _configState._cameraSettingsConfig.PanPos.ToString("0.00");
		}

		private async void TiltPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.TiltPos = (float)TiltPosSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isTiltPosDragging)
				await _cameraSliderDevice.SetTiltPosition(_configState._cameraSettingsConfig.TiltPos);
			TiltPosStatus.Content = _configState._cameraSettingsConfig.TiltPos.ToString("0.00");
		}

		private async void SlideSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.SlideSpeed = (float)SlideSpeedSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isSliderSpeedDragging)
				await _cameraSliderDevice.SetSlideSpeed(_configState._cameraSettingsConfig.SlideSpeed);
			SlideSpeedStatus.Content = _configState._cameraSettingsConfig.SlideSpeed.ToString("0.00");
		}

		private async void PanSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.PanSpeed = (float)PanSpeedSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isPanSpeedDragging)
				await _cameraSliderDevice.SetPanSpeed(_configState._cameraSettingsConfig.PanSpeed);
			PanSpeedStatus.Content = _configState._cameraSettingsConfig.PanSpeed.ToString("0.00");
		}

		private async void TiltSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.TiltSpeed = (float)TiltSpeedSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isTiltSpeedDragging)
				await _cameraSliderDevice.SetTiltSpeed(_configState._cameraSettingsConfig.TiltSpeed);
			TiltSpeedStatus.Content = _configState._cameraSettingsConfig.TiltSpeed.ToString("0.00");
		}

		private async void SlideAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.SlideAccel = (float)SlideAccelSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isSliderAccelDragging)
				await _cameraSliderDevice.SetSlideAcceleration(_configState._cameraSettingsConfig.SlideAccel);
			SlideAccelStatus.Content = _configState._cameraSettingsConfig.SlideAccel.ToString("0.00");
		}

		private async void PanAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.PanAccel = (float)PanAccelSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isPanAccelDragging)
				await _cameraSliderDevice.SetPanAcceleration(_configState._cameraSettingsConfig.PanAccel);
			PanAccelStatus.Content = _configState._cameraSettingsConfig.PanAccel.ToString("0.00");
		}

		private async void TiltAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.TiltAccel = (float)TiltAccelSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isTiltAccelDragging)
				await _cameraSliderDevice.SetTiltAcceleration(_configState._cameraSettingsConfig.TiltAccel);
			TiltAccelStatus.Content = _configState._cameraSettingsConfig.TiltAccel.ToString("0.00");
		}

		private void BtnGotoPreset_Click(object sender, RoutedEventArgs e)
		{
			if (PresetComboBox.SelectedIndex != -1)
			{
				ActivatePreset(_configState._presets[PresetComboBox.SelectedIndex]);
			}
		}

		private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void BtnEditPreset_Click(object sender, RoutedEventArgs e)
		{
			if (PresetComboBox.SelectedItem != null)
			{
				int index = PresetComboBox.SelectedIndex;
				EditPresetWindow editPresetWindow = new EditPresetWindow(_configState, index);
				editPresetWindow.Owner = this;
				editPresetWindow.Show();
				editPresetWindow.Closed += EditPresetWindow_Closed;
			}
		}

		private void BtnAddPreset_Click(object sender, RoutedEventArgs e)
		{
			EditPresetWindow editPresetWindow = new EditPresetWindow(_configState, -1);
			editPresetWindow.Owner = this;
			editPresetWindow.Show();
			editPresetWindow.Closed += EditPresetWindow_Closed;
		}

		private void EditPresetWindow_Closed(object sender, EventArgs e)
		{
			RebuildPresetComboBox();
		}

		private void BtnDeletePreset_Click(object sender, RoutedEventArgs e)
		{
			if (PresetComboBox.SelectedItem != null)
			{
				int index = PresetComboBox.SelectedIndex;
				_configState._presets.RemoveAt(index);
				PresetComboBox.Items.RemoveAt(index);
			}
		}

		private void RebuildPresetComboBox()
		{
			string oldSelectedPresetName = (string)PresetComboBox.SelectedValue ?? "";
			int newSelectedIndex = -1;

			PresetComboBox.Items.Clear();
			for (int presetIndex = 0; presetIndex < _configState._presets.Count; presetIndex++)
			{
				PresetSettings preset = _configState._presets[presetIndex];
				PresetComboBox.Items.Add(preset.PresetName);

				if (preset.PresetName == oldSelectedPresetName && newSelectedIndex == -1)
				{
					newSelectedIndex = presetIndex;
				}
			}

			if (newSelectedIndex != -1)
			{
				PresetComboBox.SelectedIndex = newSelectedIndex;
			}
			else if (PresetComboBox.SelectedIndex == -1 && _configState._presets.Count > 0)
			{
				PresetComboBox.SelectedIndex = 0;
			}
		}

		private async void BtnCalibrate_Click(object sender, RoutedEventArgs e)
		{
			if (!_deviceCalibrationRunning)
			{
				// Wait to hear if calibration started in the device event handler
				await _cameraSliderDevice.StartCalibration();
			}
		}

		private async void BtnHalt_Click(object sender, RoutedEventArgs e)
		{
			await _cameraSliderDevice.StopAllMotors();
		}

		private async void EmitLog(string txt)
		{
			await RunOnUiThread(() =>
			{
				logTextBlock.Text += txt + "\n";
			});

			Debug.WriteLine(txt);
		}

		private void SlidePosSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isSliderPosDragging = true;
		}

		private async void SlidePosSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetSlidePosition((float)((Slider)sender).Value);
			_isSliderPosDragging = false;
		}

		private void PanPosSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isPanPosDragging = true;
		}

		private async void PanPosSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetPanPosition((float)((Slider)sender).Value);
			_isPanPosDragging = false;
		}

		private void TiltPosSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isTiltPosDragging = true;
		}

		private async void TiltPosSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetTiltPosition((float)((Slider)sender).Value);
			_isTiltPosDragging = false;
		}

		private void SlideSpeedSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isSliderSpeedDragging = true;
		}

		private async void SlideSpeedSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetSlideSpeed((float)((Slider)sender).Value);
			_isSliderSpeedDragging = false;
		}

		private void PanSpeedSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isPanSpeedDragging = true;
		}

		private async void PanSpeedSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetPanSpeed((float)((Slider)sender).Value);
			_isPanSpeedDragging = false;
		}

		private void TiltSpeedSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isTiltSpeedDragging = true;
		}

		private async void TiltSpeedSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetTiltSpeed((float)((Slider)sender).Value);
			_isTiltSpeedDragging = false;
		}

		private void SlideAccelSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isSliderAccelDragging = true;
		}

		private async void SlideAccelSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetSlideAcceleration((float)((Slider)sender).Value);
			_isSliderAccelDragging = false;
		}

		private void PanAccelSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isPanAccelDragging = true;
		}

		private async void PanAccelSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetPanAcceleration((float)((Slider)sender).Value);
			_isPanAccelDragging = false;
		}

		private void TiltAccelSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isTiltAccelDragging = true;
		}

		private async void TiltAccelSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetTiltAcceleration((float)((Slider)sender).Value);
			_isTiltAccelDragging = false;
		}
	}
}
