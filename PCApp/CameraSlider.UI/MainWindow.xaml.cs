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
using CameraSlider.UI.WebSocketServer;
using System.Net;
using System.Linq;
using vtortola.WebSockets;

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

		// Web Socket Server
		private WebSocketEventListener _webSocketServer;

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
		private bool _isSpeedDragging = false;
		private bool _isAccelDragging = false;

		public MainWindow()
		{
			InitializeComponent();

			// Load the config settings first
			_configState.LoadConfig();

			// Create a	websocket server, if desired
			if (_configState._webSocketConfig.IsServerEnabled)
			{
				StartWebSocketServer();
			}

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

		// Web Socket Functions
		private void StartWebSocketServer()
		{
			if (_webSocketServer == null)
			{
				_webSocketServer = new WebSocketEventListener(
					new IPEndPoint(IPAddress.Any, _configState._webSocketConfig.ServerPort),
					new WebSocketListenerOptions() { SubProtocols=new String[]{"text"} } );
				_webSocketServer.OnConnect += OnWebSocketClientConnected;
				_webSocketServer.OnDisconnect  += OnWebSocketClientDisconnected;
				_webSocketServer.OnError += OnWebSocketClientError;
				_webSocketServer.OnMessage += OnWebSocketMessageReceived;
				_webSocketServer.Start();
			}
		}

		private void StopWebSocketServer()
		{
			if (_webSocketServer != null)
			{
				_webSocketServer.Stop();
				_webSocketServer = null;
			}
		}

		private void OnWebSocketClientConnected(WebSocket ws)
		{
			EmitLog($"Client with GUID: {ws.RemoteEndpoint.ToString()} Connected!");
		}

		private void OnWebSocketClientDisconnected(WebSocket ws)
		{
			EmitLog($"Client {ws.RemoteEndpoint.ToString()} Disconnected!");
		}

		private void OnWebSocketClientError(WebSocket ws, Exception ex)
		{
			EmitLog($"Client {ws.RemoteEndpoint.ToString()} Error: {ex.Message}");
		}

		private void OnWebSocketMessageReceived(WebSocket ws, string rawMessage)
		{
			EmitLog($"Received Message: '{rawMessage}' from client: {ws.RemoteEndpoint.ToString()}");

			string[] messageParts = rawMessage.Split(' ');
			if (messageParts.Length > 0)
			{
				switch (messageParts[0])
				{
					case "activatePreset":
					{
						if (messageParts.Length > 1)
						{
							PresetSettings preset= GetPresetByName(messageParts[1]);
							if (preset != null)
							{
								EmitLog($"Preset '{messageParts[1]}' activate requested.");
								ActivatePreset(preset);
							}
							else
							{
								EmitLog($"Preset '{messageParts[1]}' not found!");
							}
						}
					} break;

					case "getPresetList":
					{
						EmitLog($"getPresetList requested.");

						string[] presetNames= _configState._presets.Select(p => p.PresetName).ToArray();
						string response= "getPresetList "+string.Join(" ", presetNames);

						ws.WriteString(response);
					} break;
				}				
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
				await _cameraSliderDevice.SetSpeed(_configState._cameraSettingsConfig.Speed);
				await _cameraSliderDevice.SetAcceleration(_configState._cameraSettingsConfig.Accel);

				_suppressUIUpdatesToDevice = true;

				await RunOnUiThread(() =>
				{
					SlidePosSlider.Value = _configState._cameraSettingsConfig.SlidePos;
					PanPosSlider.Value = _configState._cameraSettingsConfig.PanPos;
					TiltPosSlider.Value = _configState._cameraSettingsConfig.TiltPos;
					SpeedSlider.Value = _configState._cameraSettingsConfig.Speed;
					AccelSlider.Value = _configState._cameraSettingsConfig.Accel;
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

		private PresetSettings GetPresetByName(string name)
		{
			return _configState._presets.Find(p => p.PresetName == name);
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
			PanPosSlider.Value = _configState._cameraSettingsConfig.PanPos;
			TiltPosSlider.Value = _configState._cameraSettingsConfig.TiltPos;
			SpeedSlider.Value = _configState._cameraSettingsConfig.Speed;
			AccelSlider.Value = _configState._cameraSettingsConfig.Accel;

			SlidePosStatus.Content = _configState._cameraSettingsConfig.SlidePos.ToString("0.00");
			PanPosStatus.Content = _configState._cameraSettingsConfig.PanPos.ToString("0.00");
			TiltPosStatus.Content = _configState._cameraSettingsConfig.TiltPos.ToString("0.00");
			SpeedStatus.Content = _configState._cameraSettingsConfig.Speed.ToString("0.00");
			AccelStatus.Content = _configState._cameraSettingsConfig.Accel.ToString("0.00");

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
					TiltPosSlider.IsEnabled = bEnabled;
					PanPosSlider.IsEnabled = bEnabled;
					SpeedSlider.IsEnabled = bEnabled;
					AccelSlider.IsEnabled = bEnabled;

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

		private async void Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.Speed = (float)SpeedSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isSpeedDragging)
				await _cameraSliderDevice.SetSpeed(_configState._cameraSettingsConfig.Speed);
			SpeedStatus.Content = _configState._cameraSettingsConfig.Speed.ToString("0.00");
		}

		private async void Accel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.Accel = (float)AccelSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isAccelDragging)
				await _cameraSliderDevice.SetAcceleration(_configState._cameraSettingsConfig.Accel);
			AccelStatus.Content = _configState._cameraSettingsConfig.Accel.ToString("0.00");
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
				CameraSettingsSection cameraSettings= _configState._cameraSettingsConfig;
				
				// Wait to hear if calibration started in the device event handler
				await _cameraSliderDevice.StartCalibration(
					cameraSettings.AutoSlideCalibration, 
					cameraSettings.AutoPanCalibration, 
					cameraSettings.AutoTiltCalibration);
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

		private void SpeedSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isSpeedDragging = true;
		}

		private async void SpeedSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetSpeed((float)((Slider)sender).Value);
			_isSpeedDragging = false;
		}

		private void AccelSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isAccelDragging = true;
		}

		private async void AccelSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			await _cameraSliderDevice.SetAcceleration((float)((Slider)sender).Value);
			_isAccelDragging = false;
		}

		private void BtnSetSlideMin_Click(object sender, RoutedEventArgs e)
		{

		}

		private void BtnSetSlideMax_Click(object sender, RoutedEventArgs e)
		{

		}

		private void BtnManualMoveLeft_Click(object sender, RoutedEventArgs e)
		{

		}

		private void BtnManualMoveRight_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
