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
using vtortola.WebSockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

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
		private HashSet<WebSocket> _clientWebSockets = new HashSet<WebSocket>();

		// Camera Slider
		CancellationTokenSource _messagePumpCancelSignaler = new CancellationTokenSource();
		private string _deviceName = "Camera Slider";
		private CameraSliderDevice _cameraSliderDevice;
		private bool _deviceCalibrationRunning = false;
		private bool _deviceIsMoving = false;
		private static int _deviceKeepAliveDelay = 100;
		private int _deviceKeepAliveCountdown = _deviceKeepAliveDelay;
		private bool _suppressUIUpdatesToDevice = false;

		// Preset state
		CancellationTokenSource _presetCancelSignaler = null;
		private float _presetTargetSlidePosition = 0.0f;
		private float _presetTargetPanPosition = 0.0f;
		private float _presetTargetTiltPosition = 0.0f;
		private bool _hasPendingPresetTarget = false;

		// Motor Limits
		private float _motorPanMinAngle= 0.0f;
		private float _motorPanMaxAngle= 0.0f;
		private float _motorPanMinSpeed= 0.0f;
		private float _motorPanMaxSpeed= 0.0f;
		private float _motorPanMinAcceleration= 0.0f;
		private float _motorPanMaxAcceleration= 0.0f;
		private float _motorTiltMinAngle= 0.0f;
		private float _motorTiltMaxAngle= 0.0f;
		private float _motorTiltMinSpeed= 0.0f;
		private float _motorTiltMaxSpeed= 0.0f;
		private float _motorTiltMinAcceleration= 0.0f;
		private float _motorTiltMaxAcceleration= 0.0f;
		private float _motorSlideMinSpeed= 0.0f;
		private float _motorSlideMaxSpeed= 0.0f;
		private float _motorSlideMinAcceleration= 0.0f;
		private float _motorSlideMaxAcceleration= 0.0f;

		// Slider state
		private int _sliderMinPos = 0;
		private int _sliderMaxPos = 0;
		private int _targetSlidePos = 0;
		private int _targetPanPos = 0;
		private int _targetTiltPos = 0;
		private bool _isSliderPosDragging = false;
		private bool _isPanPosDragging = false;
		private bool _isTiltPosDragging = false;
		private bool _isSpeedDragging = false;
		private bool _isAccelDragging = false;

		private ConcurrentQueue<EventArgs> _messageQueue = new ConcurrentQueue<EventArgs>();

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
			_cameraSliderDevice.SliderPosChanged += (sender, args) => { _messageQueue.Enqueue(args); };
			_cameraSliderDevice.PanPosChanged += (sender, args) => { _messageQueue.Enqueue(args); };
			_cameraSliderDevice.TiltPosChanged += (sender, args) => { _messageQueue.Enqueue(args); };
			_cameraSliderDevice.CameraDisconnected += (sender, args) => { _messageQueue.Enqueue(args); };
			_cameraSliderDevice.CameraStatusChanged += (sender, args) => { _messageQueue.Enqueue(args); };
			_cameraSliderDevice.CameraResponseHandler += (sender, args) => { _messageQueue.Enqueue(args); };

			// Setup the UI
			SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, true);

			// Apply the config settings to the UI (without trying to send them to the device)
			ApplyConfigToUI();

			// Kick off the watchdog workers
			_ = StartMessagePump(_messagePumpCancelSignaler.Token);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			// Stop any running async tasks
			_messagePumpCancelSignaler.Cancel();
			if (_presetCancelSignaler != null)
			{
				_presetCancelSignaler.Cancel();
			}

			// Disconnect from everything
			if (_cameraSliderDevice.IsSetupAndConnected)
			{
				_cameraSliderDevice.Disconnect();
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
				_clientWebSockets.Clear();
			}
		}

		private void OnWebSocketClientConnected(WebSocket ws)
		{
			EmitLog($"Client with GUID: {ws.RemoteEndpoint.ToString()} Connected!");
			SetWebsocketStatusLabel("Connected");
			_clientWebSockets.Add(ws);
		}

		private void OnWebSocketClientDisconnected(WebSocket ws)
		{
			EmitLog($"Client {ws.RemoteEndpoint.ToString()} Disconnected!");
			SetWebsocketStatusLabel("Disconnected");
			_clientWebSockets.Remove(ws);
		}

		private void OnWebSocketClientError(WebSocket ws, Exception ex)
		{
			string remoteEndpoint = ws != null ? ws.RemoteEndpoint.ToString() : "Unknown";
			EmitLog($"Client {remoteEndpoint} Error: {ex.Message}");
			if (ws != null)
			{
				_clientWebSockets.Remove(ws);
			}
		}

		internal class ActivatePresetArgs : EventArgs
		{
			public string PresetName
			{
				get; set;
			}
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
							_messageQueue.Enqueue(new ActivatePresetArgs() { PresetName=messageParts[1] });
						}

						// TODO: Check if the preset name is valid
						ws.WriteString("activatePreset ok");
					} break;

					case "getPresetList":
					{
						EmitLog($"getPresetList requested.");

						ws.WriteString("getPresetList " + _configState.PresetNameListString);
					} break;

					case "getCameraConnectionStatus":
					{
						EmitLog($"getCameraConnectionStatus requested.");

						string statusString= _cameraSliderDevice.IsSetupAndConnected ? "connected" : "disconnected";
						ws.WriteString($"getCameraConnectionStatus {statusString}");
					} break;

					case "getCameraMoveStatus":
					{
						EmitLog($"getCameraMoveStatus requested.");

						string statusString = _deviceIsMoving ? "moving" : "idle";
						ws.WriteString($"getCameraMoveStatus {statusString}");
					}
					break;

					case "getCameraCalibrationStatus":
					{
						EmitLog($"getCameraCalibrationStatus requested.");

						string statusString = _deviceCalibrationRunning ? "calibrating" : "idle";
						ws.WriteString($"getCameraMoveStatus {statusString}");
					}
					break;
				}
			}
		}

		private void BroadcastMessageToAllWebSocketClients(string message)
		{
			foreach (var ws in _clientWebSockets)
			{
				ws.WriteString(message);
			}
		}

		// UI Functions
		private async void SetWebsocketStatusLabel(string status)
		{
			await RunOnUiThread(() =>
			{
				WebsocketTxtStatus.Content = status;
			});
		}

		private async void SetCameraPositionSliders(float slidePos, float panPos, float tiltPos)
		{
			await RunOnUiThread(() =>
			{
				SlidePosSlider.Value = slidePos;
				PanPosSlider.Value = panPos;
				TiltPosSlider.Value = tiltPos;
			});
		}

		private async void SetCameraSpeedSliders(float speed, float accel)
		{
			await RunOnUiThread(() =>
			{
				SpeedSlider.Value = speed;
				AccelSlider.Value = accel;
			});
		}

		private async void SetLabelValue(Label label, string value)
		{
			await RunOnUiThread(() =>
			{
				label.Content = value;
			});
		}

		private async void SetTextBoxValue(TextBox textBox, string value)
		{
			await RunOnUiThread(() =>
			{
				textBox.Text = value;
			});
		}

		private void SetCameraStatusLabel(string status)
		{
			SetLabelValue(CameraTxtStatus, status);
		}

		private void SetCameraSliderPositionLabel(int value)
		{
			SetLabelValue(LblSliderRawPos, value.ToString());
		}
		private void SetCameraSliderMinPositionLabel(int value)
		{
			SetLabelValue(LblSlideMin, value.ToString());
		}
		private void SetCameraSliderMaxPositionLabel(int value)
		{
			SetLabelValue(LblSlideMax, value.ToString());
		}

		private void SetMotorPanMinAngleLabel(float minAngle)
		{
			SetTextBoxValue(PanMinAngleTextBox, minAngle.ToString());
		}
		private void SetMotorPanMaxAngleLabel(float maxAngle)
		{
			SetTextBoxValue(PanMaxAngleTextBox, maxAngle.ToString());
		}
		private void SetMotorPanMinSpeedLabel(float minSpeed)
		{
			SetTextBoxValue(PanMinSpeedTextBox, minSpeed.ToString());
		}
		private void SetMotorPanMaxSpeedLabel(float maxSpeed)
		{
			SetTextBoxValue(PanMaxSpeedTextBox, maxSpeed.ToString());
		}
		private void SetMotorPanMinAccelerationLabel(float minAcceleration)
		{
			SetTextBoxValue(PanMinAccelerationTextBox, minAcceleration.ToString());
		}
		private void SetMotorPanMaxAccelerationLabel(float maxAcceleration)
		{
			SetTextBoxValue(PanMaxAccelerationTextBox, maxAcceleration.ToString());
		}

		private void SetMotorTiltMinAngleLabel(float minAngle)
		{
			SetTextBoxValue(TiltMinAngleTextBox, minAngle.ToString());
		}
		private void SetMotorTiltMaxAngleLabel(float maxAngle)
		{
			SetTextBoxValue(TiltMaxAngleTextBox, maxAngle.ToString());
		}
		private void SetMotorTiltMinSpeedLabel(float minSpeed)
		{
			SetTextBoxValue(TiltMinSpeedTextBox, minSpeed.ToString());
		}
		private void SetMotorTiltMaxSpeedLabel(float maxSpeed)
		{
			SetTextBoxValue(TiltMaxSpeedTextBox, maxSpeed.ToString());
		}
		private void SetMotorTiltMinAccelerationLabel(float minAcceleration)
		{
			SetTextBoxValue(TiltMinAccelerationTextBox, minAcceleration.ToString());
		}
		private void SetMotorTiltMaxAccelerationLabel(float maxAcceleration)
		{
			SetTextBoxValue(TiltMaxAccelerationTextBox, maxAcceleration.ToString());
		}

		private void SetMotorSlideMinSpeedLabel(float minSpeed)
		{
			SetTextBoxValue(SlideMinSpeedTextBox, minSpeed.ToString());
		}
		private void SetMotorSlideMaxSpeedLabel(float maxSpeed)
		{
			SetTextBoxValue(SlideMaxSpeedTextBox, maxSpeed.ToString());
		}
		private void SetMotorSlideMinAccelerationLabel(float minAcceleration)
		{
			SetTextBoxValue(SlideMinAccelerationTextBox, minAcceleration.ToString());
		}
		private void SetMotorSlideMaxAccelerationLabel(float maxAcceleration)
		{
			SetTextBoxValue(SlideMaxAccelerationTextBox, maxAcceleration.ToString());
		}

		private void CameraSliderResponseReceived(object sender, CameraResponseArgs evt)
		{
			EmitLog("Received slider response received: " + evt.Args.ToString());
			string[] args= evt.Args;

			if (args.Length > 0)
			{
				switch (args[0])
				{
				case "pong":
				{
					// Nothing to do here
				}
				break;
				case "slider_calibration":
				{
					if (args.Length >= 3)
					{
						_sliderMinPos= int.Parse(args[1]);
						_sliderMaxPos= int.Parse(args[2]);

						SetCameraSliderMinPositionLabel(_sliderMinPos);
						SetCameraSliderMaxPositionLabel(_sliderMaxPos);
					}
				}
				break;
				case "motor_pan_limits":
				{
					if (args.Length >= 7)
					{
						_motorPanMinAngle= float.Parse(args[1]);
						_motorPanMaxAngle= float.Parse(args[2]);
						_motorPanMinSpeed = float.Parse(args[3]);
						_motorPanMaxSpeed = float.Parse(args[4]);
						_motorPanMinAcceleration = float.Parse(args[5]);
						_motorPanMaxAcceleration = float.Parse(args[6]);

						SetMotorPanMinAngleLabel(_motorPanMinAngle);
						SetMotorPanMaxAngleLabel(_motorPanMaxAngle);
						SetMotorPanMinSpeedLabel(_motorPanMinSpeed);
						SetMotorPanMaxSpeedLabel(_motorPanMaxSpeed);
						SetMotorPanMinAccelerationLabel(_motorPanMinAcceleration);
						SetMotorPanMaxAccelerationLabel(_motorPanMaxAcceleration);
					}
				}
				break;
				case "motor_tilt_limits":
				{
					if (args.Length >= 7)
					{
						_motorTiltMinAngle = float.Parse(args[1]);
						_motorTiltMaxAngle = float.Parse(args[2]);
						_motorTiltMinSpeed = float.Parse(args[3]);
						_motorTiltMaxSpeed = float.Parse(args[4]);
						_motorTiltMinAcceleration = float.Parse(args[5]);
						_motorTiltMaxAcceleration = float.Parse(args[6]);

						SetMotorTiltMinAngleLabel(_motorTiltMinAngle);
						SetMotorTiltMaxAngleLabel(_motorTiltMaxAngle);
						SetMotorTiltMinSpeedLabel(_motorTiltMinSpeed);
						SetMotorTiltMaxSpeedLabel(_motorTiltMaxSpeed);
						SetMotorTiltMinAccelerationLabel(_motorTiltMinAcceleration);
						SetMotorTiltMaxAccelerationLabel(_motorTiltMaxAcceleration);
					}
				}
				break;
				case "motor_slide_limits":
				{
					if (args.Length >= 5)
					{
						_motorSlideMinSpeed = float.Parse(args[1]);
						_motorSlideMaxSpeed = float.Parse(args[2]);
						_motorSlideMinAcceleration = float.Parse(args[3]);
						_motorSlideMaxAcceleration = float.Parse(args[4]);

						SetMotorSlideMinSpeedLabel(_motorSlideMinSpeed);
						SetMotorSlideMaxSpeedLabel(_motorSlideMaxSpeed);
						SetMotorSlideMinAccelerationLabel(_motorSlideMinAcceleration);
						SetMotorSlideMaxAccelerationLabel(_motorSlideMaxAcceleration);
					}
				}
				break;
				case "slide_min_set":
				{
					_sliderMinPos= args.Length >= 2 ? int.Parse(args[1]) : 0;
					SetCameraSliderMinPositionLabel(_sliderMinPos);
				}
				break;
				case "slide_max_set":
				{
					_sliderMaxPos = args.Length >= 2 ? int.Parse(args[1]) : 0;
					SetCameraSliderMaxPositionLabel(_sliderMaxPos);
				}
				break;
				}
			}
		}

		private void OnCameraStatusChanged(object sender, CameraStatusChangedEventArgs e)
		{
			EmitLog("Received slider event received: " + e.Status);

			switch (e.Status)
			{
			case "move_start":
			{
				_deviceIsMoving = true;
				SetCameraStatusLabel("Moving...");
				BroadcastMessageToAllWebSocketClients("moveStatusChanged moving");
			}
			break;
			case "move_complete":
			{
				_hasPendingPresetTarget = false;
				_deviceIsMoving = false;
				SetCameraStatusLabel("Idle");
				BroadcastMessageToAllWebSocketClients("moveStatusChanged idle");
			}
			break;
			case "calibration_started":
			{
				_deviceCalibrationRunning = true;
				SetUIControlsDisableFlag(UIControlDisableFlags.Calibrating, true);
				SetCameraStatusLabel("Calibrating...");
				BroadcastMessageToAllWebSocketClients("calibrationStatusChanged calibrating");
			}
			break;
			case "calibration_completed":
			{
				_deviceCalibrationRunning = false;
				SetUIControlsDisableFlag(UIControlDisableFlags.Calibrating, false);
				SetCameraStatusLabel("Idle");
				BroadcastMessageToAllWebSocketClients("calibrationStatusChanged completed");

				// Refetch the slider calibration to update the UI
				_cameraSliderDevice.GetSliderCalibration();
			}
			break;
			case "calibration_failed":
			{
				_deviceCalibrationRunning = false;
				SetUIControlsDisableFlag(UIControlDisableFlags.Calibrating, false);
				SetCameraStatusLabel("Calibration Failed!");
				BroadcastMessageToAllWebSocketClients("calibrationStatusChanged failed");
			}
			break;
			}
		}

		private void TiltPosChanged(object sender, CameraPositionChangedEventArgs e)
		{
			_targetTiltPos = e.Value;
		}

		private void PanPosChanged(object sender, CameraPositionChangedEventArgs e)
		{
			_targetPanPos = e.Value;
		}

		private void SliderPosChanged(object sender, CameraPositionChangedEventArgs e)
		{
			_targetSlidePos = e.Value;
			SetCameraSliderPositionLabel(_targetSlidePos);
		}

		private void OnCameraSliderConnected()
		{
			EmitLog("CameraSlider connected");

			SetCameraStatusLabel("Idle");
			SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, false);

			// Get the slider calibration from the device (response updates the	UI)	
			_cameraSliderDevice.GetSliderCalibration();

			// Get the motor angle, speed, and acceleration limits from the device 
			// (response updates the UI)
			_cameraSliderDevice.GetMotorPanLimits();
			_cameraSliderDevice.GetMotorTiltLimits();
			_cameraSliderDevice.GetMotorSlideLimits();

			// Send the desired speed, accel, and position params to the device
			_cameraSliderDevice.SetSpeed(_configState._cameraSettingsConfig.Speed);
			_cameraSliderDevice.SetAcceleration(_configState._cameraSettingsConfig.Accel);
			_cameraSliderDevice.SetPosition(
					_configState._cameraSettingsConfig.SlidePos,
					_configState._cameraSettingsConfig.PanPos,
					_configState._cameraSettingsConfig.TiltPos);

			// Get the actual target raw slider position from the device
			_targetSlidePos = _cameraSliderDevice.GetSlidePosition();
			SetCameraSliderPositionLabel(_targetSlidePos);

			_suppressUIUpdatesToDevice = true;
			SetCameraPositionSliders(
				_configState._cameraSettingsConfig.SlidePos,
				_configState._cameraSettingsConfig.PanPos,
				_configState._cameraSettingsConfig.TiltPos);
			SetCameraSpeedSliders(
				_configState._cameraSettingsConfig.Speed,
				_configState._cameraSettingsConfig.Accel);
			_suppressUIUpdatesToDevice = false;

			BroadcastMessageToAllWebSocketClients("cameraConnectionStatusChanged connected");
		}

		private void OnCameraSliderDisconnected(object sender, DisconnectedEventArgs args)
		{
			EmitLog("CameraSlider Disconnected");

			SetCameraStatusLabel("Searching...");
			SetActivePresetStatusLabel("");
			SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, true);

			BroadcastMessageToAllWebSocketClients("cameraConnectionStatusChanged disconnected");
		}

		private async Task StartMessagePump(CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					// Ping a connected device regularly to keep it awake
					if (_cameraSliderDevice.IsSetupAndConnected)
					{
						_deviceKeepAliveCountdown--;
						if (_deviceKeepAliveCountdown <= 0)
						{
							_cameraSliderDevice.WakeUp();
							_deviceKeepAliveCountdown = _deviceKeepAliveDelay;
						}
					}
					// Attempt to kick off an async device reconnection
					else
					{
						try
						{
							if (_cameraSliderDevice.Connect(_deviceName))
							{
								OnCameraSliderConnected();
							}
						}
						catch (Exception ex)
						{
							EmitLog("Device connect error: " + ex.Message);
						}
					}

					// Send next pending command to the device
					if (_cameraSliderDevice.IsSetupAndConnected)
					{
						_cameraSliderDevice.SendNextPendingCommand();
					}

					// Process any enqueued messages from the device or websocket threads
					ProcessMessageQueue();

					// Save the config settings if they are dirty
					if (_configState._areConfigSettingsDirty)
					{
						_configState.SaveConfig();
					}

					await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
				}
			}
			catch (OperationCanceledException)
			{
				// Handle the cancellation
				Console.WriteLine("Shut down watch dog worker");
			}
		}

		private void ProcessMessageQueue()
		{
			while (_messageQueue.TryDequeue(out EventArgs evt))
			{
				if (evt is DisconnectedEventArgs)
				{
					OnCameraSliderDisconnected(this, evt as DisconnectedEventArgs);
				}
				else if (evt is CameraResponseArgs)
				{
					CameraSliderResponseReceived(this, evt as CameraResponseArgs);
				}
				else if (evt is CameraStatusChangedEventArgs)
				{
					OnCameraStatusChanged(this, evt as CameraStatusChangedEventArgs);
				}
				else if (evt is CameraPositionChangedEventArgs)
				{
					var posEvt = evt as CameraPositionChangedEventArgs;

					switch (posEvt.Type)
					{
					case CameraPositionChangedEventArgs.PositionType.Slider:
						SliderPosChanged(this, posEvt);
						break;
					case CameraPositionChangedEventArgs.PositionType.Pan:
						PanPosChanged(this, posEvt);
						break;
					case CameraPositionChangedEventArgs.PositionType.Tilt:
						TiltPosChanged(this, posEvt);
						break;
					}
				}
				else if (evt is ActivatePresetArgs)
				{
					var presetArgs = evt as ActivatePresetArgs;
					ActivatePreset(GetPresetByName(presetArgs.PresetName));
				}
			}
		}

		private PresetSettings GetPresetByName(string name)
		{
			return _configState._presets.Find(p => p.PresetName == name);
		}

		private void ActivatePreset(PresetSettings preset)
		{
			if (preset == null)
			{
				return;
			}

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
			_cameraSliderDevice.SetPosition(
				_presetTargetSlidePosition, 
				_presetTargetPanPosition, 
				_presetTargetTiltPosition);

			_suppressUIUpdatesToDevice= true;
			await RunOnUiThread(() =>
			{
				SlidePosSlider.Value = preset.SlidePosition;
				PanPosSlider.Value = preset.PanPosition;
				TiltPosSlider.Value = preset.TiltPosition;
			});
			_suppressUIUpdatesToDevice= false;

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
			_suppressUIUpdatesToDevice = true;

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

			PitchEnabledCheckBox.IsChecked= _configState._cameraSettingsConfig.AutoTiltCalibration;
			PanEnabledCheckBox.IsChecked= _configState._cameraSettingsConfig.AutoPanCalibration;
			SlideEnabledCheckBox.IsChecked= _configState._cameraSettingsConfig.AutoSlideCalibration;

			ManualMoveAmountTextBox.Text = _configState._cameraSettingsConfig.ManualSlideStepSize.ToString();

			RebuildPresetComboBox();

			_suppressUIUpdatesToDevice = false;
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
				_uiDisableBitmask = newFlags;

				await RunOnUiThread(() =>
				{
					SlidePosSlider.IsEnabled = bEnabled;
					TiltPosSlider.IsEnabled = bEnabled;
					PanPosSlider.IsEnabled = bEnabled;
					SpeedSlider.IsEnabled = bEnabled;
					AccelSlider.IsEnabled = bEnabled;

					BtnCalibrate.IsEnabled = bEnabled;
					BtnResetCalibration.IsEnabled = bEnabled;
					BtnHalt.IsEnabled = bEnabled;

					BtnAddPreset.IsEnabled = bEnabled;
					BtnEditPreset.IsEnabled = bEnabled;
					BtnDeletePreset.IsEnabled = bEnabled;
					BtnGotoPreset.IsEnabled = bEnabled;

					BtnManualMoveLeft.IsEnabled = bEnabled;
					BtnManualMoveRight.IsEnabled = bEnabled;
					BtnSetSlideMax.IsEnabled = bEnabled;
					BtnSetSlideMin.IsEnabled = bEnabled;
				});
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

		private void ApplySlidePanTiltPositionsFromConfig()
		{
			_cameraSliderDevice.SetPosition(
				_configState._cameraSettingsConfig.SlidePos,
				_configState._cameraSettingsConfig.PanPos,
				_configState._cameraSettingsConfig.TiltPos);
		}

		private void SlidePos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.SlidePos = (float)SlidePosSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isSliderPosDragging)
				ApplySlidePanTiltPositionsFromConfig();
			SlidePosStatus.Content = _configState._cameraSettingsConfig.SlidePos.ToString("0.00");
		}

		private void PanPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.PanPos = (float)PanPosSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isPanPosDragging)
				ApplySlidePanTiltPositionsFromConfig();
			PanPosStatus.Content = _configState._cameraSettingsConfig.PanPos.ToString("0.00");
		}

		private void TiltPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.TiltPos = (float)TiltPosSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isTiltPosDragging)
				ApplySlidePanTiltPositionsFromConfig();
			TiltPosStatus.Content = _configState._cameraSettingsConfig.TiltPos.ToString("0.00");
		}

		private void Speed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.Speed = (float)SpeedSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isSpeedDragging)
				_cameraSliderDevice.SetSpeed(_configState._cameraSettingsConfig.Speed);
			SpeedStatus.Content = _configState._cameraSettingsConfig.Speed.ToString("0.00");
		}

		private void Accel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_configState._cameraSettingsConfig.Accel = (float)AccelSlider.Value;
			_configState._areConfigSettingsDirty = true;
			if (!_suppressUIUpdatesToDevice && !_isAccelDragging)
				_cameraSliderDevice.SetAcceleration(_configState._cameraSettingsConfig.Accel);
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
				_configState._arePresetsDirty = true;
				_configState._areConfigSettingsDirty= true;
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

		private void BtnCalibrate_Click(object sender, RoutedEventArgs e)
		{
			if (!_deviceCalibrationRunning)
			{
				CameraSettingsSection cameraSettings= _configState._cameraSettingsConfig;
				
				// Wait to hear if calibration started in the device event handler
				_cameraSliderDevice.StartCalibration(
					cameraSettings.AutoSlideCalibration, 
					cameraSettings.AutoPanCalibration, 
					cameraSettings.AutoTiltCalibration);
			}
		}

		private void BtnResetCalibration_Click(object sender, RoutedEventArgs e)
		{
			if (!_deviceCalibrationRunning)
			{
				_cameraSliderDevice.ResetCalibration();

				// Zero out the saved slider positions in the config
				_configState._cameraSettingsConfig.SlidePos= 0.0f;
				_configState._cameraSettingsConfig.PanPos= 0.0f;
				_configState._cameraSettingsConfig.TiltPos= 0.0f;
				_configState._areConfigSettingsDirty= true;

				// Update the UI
				_suppressUIUpdatesToDevice= true;
				SetCameraPositionSliders(
					_configState._cameraSettingsConfig.SlidePos,
					_configState._cameraSettingsConfig.PanPos, 
					_configState._cameraSettingsConfig.TiltPos);
				_suppressUIUpdatesToDevice= false;

				// Refetch the slider state to update the UI
				_cameraSliderDevice.GetSliderCalibration();
			}
		}

		private void BtnHalt_Click(object sender, RoutedEventArgs e)
		{
			_cameraSliderDevice.StopAllMotors();
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

		private void SlidePosSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			_configState._cameraSettingsConfig.SlidePos= (float)((Slider)sender).Value;
			_configState._areConfigSettingsDirty = true;
			ApplySlidePanTiltPositionsFromConfig();
			_isSliderPosDragging = false;
		}

		private void PanPosSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isPanPosDragging = true;
		}

		private void PanPosSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			_configState._cameraSettingsConfig.PanPos = (float)((Slider)sender).Value;
			_configState._areConfigSettingsDirty = true;
			ApplySlidePanTiltPositionsFromConfig();
			_isPanPosDragging = false;
		}

		private void TiltPosSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isTiltPosDragging = true;
		}

		private void TiltPosSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			_configState._cameraSettingsConfig.TiltPos = (float)((Slider)sender).Value;
			_configState._areConfigSettingsDirty = true;
			ApplySlidePanTiltPositionsFromConfig();
			_isTiltPosDragging = false;
		}

		private void SpeedSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isSpeedDragging = true;
		}

		private void SpeedSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			_cameraSliderDevice.SetSpeed((float)((Slider)sender).Value);
			_isSpeedDragging = false;
		}

		private void AccelSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
		{
			_isAccelDragging = true;
		}

		private void AccelSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
		{
			_cameraSliderDevice.SetAcceleration((float)((Slider)sender).Value);
			_isAccelDragging = false;
		}

		private void BtnSetSlideMin_Click(object sender, RoutedEventArgs e)
		{
			_cameraSliderDevice.SaveSlideMin();
		}

		private void BtnSetSlideMax_Click(object sender, RoutedEventArgs e)
		{
			_cameraSliderDevice.SaveSlideMax();
		}

		private void BtnManualMoveLeft_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(ManualMoveAmountTextBox.Text, out int moveAmount))
			{
				_cameraSliderDevice.MoveSlider(moveAmount);
			}
		}

		private void BtnManualMoveRight_Click(object sender, RoutedEventArgs e)
		{
			if (int.TryParse(ManualMoveAmountTextBox.Text, out int moveAmount))
			{
				_cameraSliderDevice.MoveSlider(-moveAmount);
			}
		}

		private void ManualMoveAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (int.TryParse(ManualMoveAmountTextBox.Text, out int moveAmount))
			{
				_configState._cameraSettingsConfig.ManualSlideStepSize = moveAmount;
				_configState._areConfigSettingsDirty= true;
			}
		}

		private void PitchEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			_configState._cameraSettingsConfig.AutoTiltCalibration= (bool)PitchEnabledCheckBox.IsChecked;
			_configState._areConfigSettingsDirty= true;
		}

		private void PanEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			_configState._cameraSettingsConfig.AutoPanCalibration = (bool)PanEnabledCheckBox.IsChecked;
			_configState._areConfigSettingsDirty = true;
		}

		private void SlideEnabledCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			_configState._cameraSettingsConfig.AutoSlideCalibration = (bool)SlideEnabledCheckBox.IsChecked;
			_configState._areConfigSettingsDirty = true;
		}

		private bool FetchLimitTextBoxValue(TextBox textBox, float currentValue, out float newValue)
		{
			if (float.TryParse(textBox.Text, out newValue))
			{
				return Math.Abs(newValue - currentValue) > 0.1f;
			}
			return false;
		}

		private void BtnApplyMotorLimits_Click(object sender, RoutedEventArgs e)
		{
			// Check if the pan limits have changed
			bool bHasPanLimitsChanged = false;
			float newPanMinAngle, newPanMaxAngle;
			float newPanMinSpeed, newPanMaxSpeed;
			float newPanMinAcceleration, newPanMaxAcceleration;
			bHasPanLimitsChanged|= FetchLimitTextBoxValue(PanMinAngleTextBox, _motorPanMinAngle, out newPanMinAngle);
			bHasPanLimitsChanged|= FetchLimitTextBoxValue(PanMaxAngleTextBox, _motorPanMaxAngle, out newPanMaxAngle);
			bHasPanLimitsChanged|= FetchLimitTextBoxValue(PanMinSpeedTextBox, _motorPanMinSpeed, out newPanMinSpeed);
			bHasPanLimitsChanged|= FetchLimitTextBoxValue(PanMaxSpeedTextBox, _motorPanMaxSpeed, out newPanMaxSpeed);
			bHasPanLimitsChanged|= FetchLimitTextBoxValue(PanMinAccelerationTextBox, _motorPanMinAcceleration, out newPanMinAcceleration);
			bHasPanLimitsChanged|= FetchLimitTextBoxValue(PanMaxAccelerationTextBox, _motorPanMaxAcceleration, out newPanMaxAcceleration);
			if (bHasPanLimitsChanged)
			{
				_cameraSliderDevice.SetPanMotorLimits(
					newPanMinAngle, newPanMaxAngle,
					newPanMinSpeed, newPanMaxSpeed,
					newPanMinAcceleration, newPanMaxAcceleration);
				_cameraSliderDevice.GetMotorPanLimits();
			}

			// Check if the tilt limits have changed
			bool bHasTiltLimitsChanged = false;
			float newTiltMinAngle, newTiltMaxAngle;
			float newTiltMinSpeed, newTiltMaxSpeed;
			float newTiltMinAcceleration, newTiltMaxAcceleration;
			bHasTiltLimitsChanged|= FetchLimitTextBoxValue(TiltMinAngleTextBox, _motorTiltMinAngle, out newTiltMinAngle);
			bHasTiltLimitsChanged|= FetchLimitTextBoxValue(TiltMaxAngleTextBox, _motorTiltMaxAngle, out newTiltMaxAngle);
			bHasTiltLimitsChanged|= FetchLimitTextBoxValue(TiltMinSpeedTextBox, _motorTiltMinSpeed, out newTiltMinSpeed);
			bHasTiltLimitsChanged|= FetchLimitTextBoxValue(TiltMaxSpeedTextBox, _motorTiltMaxSpeed, out newTiltMaxSpeed);
			bHasTiltLimitsChanged|= FetchLimitTextBoxValue(TiltMinAccelerationTextBox, _motorTiltMinAcceleration, out newTiltMinAcceleration);
			bHasTiltLimitsChanged|= FetchLimitTextBoxValue(TiltMaxAccelerationTextBox, _motorTiltMaxAcceleration, out newTiltMaxAcceleration);
			if (bHasTiltLimitsChanged)
			{
				_cameraSliderDevice.SetTiltMotorLimits(
					newTiltMinAngle, newTiltMaxAngle,
					newTiltMinSpeed, newTiltMaxSpeed,
					newTiltMinAcceleration, newTiltMaxAcceleration);
				_cameraSliderDevice.GetMotorTiltLimits();
			}

			// Check if the slide limits have changed
			bool bHasSlideLimitsChanged = false;
			float newSlideMinSpeed, newSlideMaxSpeed;
			float newSlideMinAcceleration, newSlideMaxAcceleration;
			bHasSlideLimitsChanged|= FetchLimitTextBoxValue(SlideMinSpeedTextBox, _motorSlideMinSpeed, out newSlideMinSpeed);
			bHasSlideLimitsChanged|= FetchLimitTextBoxValue(SlideMaxSpeedTextBox, _motorSlideMaxSpeed, out newSlideMaxSpeed);
			bHasSlideLimitsChanged|= FetchLimitTextBoxValue(SlideMinAccelerationTextBox, _motorSlideMinAcceleration, out newSlideMinAcceleration);
			bHasSlideLimitsChanged|= FetchLimitTextBoxValue(SlideMaxAccelerationTextBox, _motorSlideMaxAcceleration, out newSlideMaxAcceleration);
			if (bHasSlideLimitsChanged)
			{
				_cameraSliderDevice.SetSlideMotorLimits(
					newSlideMinSpeed, newSlideMaxSpeed,
					newSlideMinAcceleration, newSlideMaxAcceleration);
				_cameraSliderDevice.GetMotorSlideLimits();
			}
		}

		private void BtnRefreshMotorLimits_Click(object sender, RoutedEventArgs e)
		{
			_cameraSliderDevice.GetMotorPanLimits();
			_cameraSliderDevice.GetMotorTiltLimits();
			_cameraSliderDevice.GetMotorSlideLimits();
		}

		private void BtnResetMotorLimits_Click(object sender, RoutedEventArgs e)
		{
			_cameraSliderDevice.ResetMotorLimits();
			_cameraSliderDevice.GetMotorPanLimits();
			_cameraSliderDevice.GetMotorTiltLimits();
			_cameraSliderDevice.GetMotorSlideLimits();
		}
	}
}
