using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using CameraSlider.Bluetooth.Events;
using System.ComponentModel;
using Color = System.Drawing.Color;
using CameraSlider.Bluetooth;
using CameraSlider.Bluetooth.Schema;
using System.Threading;
using System.Configuration;
using System.Text.RegularExpressions;
using OBSWebsocketDotNet;
using TwitchLib.PubSub;
using TwitchLib.Api.Interfaces;
using TwitchLib.Api;
using TwitchLib.PubSub.Events;
using System.Collections.Generic;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Client.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using Newtonsoft.Json;
using OBSWebsocketDotNet.Types;

namespace CameraSlider.UI
{
	public class ObsConfigSection : ConfigurationSection
	{
		public ObsConfigSection()
		{
			PatternSceneName = "Main";
			PatternSceneDuration = 3000;
			SocketAddress = "127.0.0.1:4444";
			Password = "";
		}

		[ConfigurationProperty("pattern_scene_name")]
		public string PatternSceneName
		{
			get { return (string)this["pattern_scene_name"]; }
			set { this["pattern_scene_name"] = value; }
		}

		[ConfigurationProperty("pattern_scene_duration")]
		public int PatternSceneDuration
		{
			get { return (int)this["pattern_scene_duration"]; }
			set { this["pattern_scene_duration"] = value; }
		}

		[ConfigurationProperty("socket_address")]
		public string SocketAddress
		{
			get { return (string)this["socket_address"]; }
			set { this["socket_address"] = value; }
		}

		[ConfigurationProperty("password")]
		public string Password
		{
			get { return (string)this["password"]; }
			set { this["password"] = value; }
		}
	}

	public class TwitchWebAPISection : ConfigurationSection
	{
		public TwitchWebAPISection()
		{
			ChannelID = "";
			ClientID = "";
			Secret = "";
		}

		[ConfigurationProperty("channel_id")]
		public string ChannelID
		{
			get { return (string)this["channel_id"]; }
			set { this["channel_id"] = value; }
		}

		[ConfigurationProperty("client_id")]
		public string ClientID
		{
			get { return (string)this["client_id"]; }
			set { this["client_id"] = value; }
		}

		[ConfigurationProperty("secret")]
		public string Secret
		{
			get { return (string)this["secret"]; }
			set { this["secret"] = value; }
		}
	}

	public class TriggerSettings
	{
		public string TriggerName { get; set; }
		public bool IsModOnly { get; set; }
		public bool IsActive { get; set; }

		public TriggerSettings()
		{
			TriggerName = "";
			IsModOnly = false;
			IsActive = false;
		}

		public TriggerSettings(TriggerSettings other)
		{
			TriggerName = other.TriggerName;
			IsModOnly = other.IsModOnly;
			IsActive = other.IsActive;
		}
	}

	public class PresetSettings
	{
		public string PresetName { get; set; }
		public float SlidePosition { get; set; }
		public float PanPosition { get; set; }
		public float TiltPosition { get; set; }
		public String ObsScene { get; set; }
		public TriggerSettings ChatTrigger { get; set; }
		public TriggerSettings RedeemTrigger { get; set; }

		public PresetSettings()
		{
			PresetName = "";
			SlidePosition = 0;
			PanPosition = 0;
			TiltPosition = 0;
			ObsScene = "";
			ChatTrigger = new TriggerSettings();
			RedeemTrigger = new TriggerSettings();
		}

		public PresetSettings(PresetSettings other)
		{
			PresetName = other.PresetName;
			SlidePosition = other.SlidePosition;
			PanPosition = other.PanPosition;
			TiltPosition = other.TiltPosition;
			ObsScene = other.ObsScene;
			ChatTrigger = new TriggerSettings(other.ChatTrigger);
			RedeemTrigger = new TriggerSettings(other.RedeemTrigger);
		}
	}

	public class CameraSettingsSection : ConfigurationSection
	{
		public CameraSettingsSection()
		{
			SlidePos = 0.0f;
			SlideSpeed = 0.0f;
			SlideAccel = 0.0f;
			PanPos = 0.0f;
			PanSpeed = 0.0f;
			PanAccel = 0.0f;
			TiltPos = 0.0f;
			TiltSpeed = 0.0f;
			TiltAccel = 0.0f;
			PresetJson = "";
		}

		[ConfigurationProperty("slide_pos")]
		public float SlidePos
		{
			get { return (float)this["slide_pos"]; }
			set { this["slide_pos"] = value; }
		}

		[ConfigurationProperty("slide_speed")]
		public float SlideSpeed
		{
			get { return (float)this["slide_speed"]; }
			set { this["slide_speed"] = value; }
		}

		[ConfigurationProperty("slide_accel")]
		public float SlideAccel
		{
			get { return (float)this["slide_accel"]; }
			set { this["slide_accel"] = value; }
		}

		[ConfigurationProperty("pan_pos")]
		public float PanPos
		{
			get { return (float)this["pan_pos"]; }
			set { this["pan_pos"] = value; }
		}

		[ConfigurationProperty("pan_speed")]
		public float PanSpeed
		{
			get { return (float)this["pan_speed"]; }
			set { this["pan_speed"] = value; }
		}

		[ConfigurationProperty("pan_accel")]
		public float PanAccel
		{
			get { return (float)this["pan_accel"]; }
			set { this["pan_accel"] = value; }
		}

		[ConfigurationProperty("tilt_pos")]
		public float TiltPos
		{
			get { return (float)this["tilt_pos"]; }
			set { this["tilt_pos"] = value; }
		}

		[ConfigurationProperty("tilt_speed")]
		public float TiltSpeed
		{
			get { return (float)this["tilt_speed"]; }
			set { this["tilt_speed"] = value; }
		}

		[ConfigurationProperty("tilt_accel")]
		public float TiltAccel
		{
			get { return (float)this["tilt_accel"]; }
			set { this["tilt_accel"] = value; }
		}

		[ConfigurationProperty("preset_json")]
		public string PresetJson
		{
			get { return (string)this["preset_json"]; }
			set { this["preset_json"] = value; }
		}
	}

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
		private Configuration _configFile;
		private TwitchWebAPISection _twitchWebApiConfig;
		private ObsConfigSection _obsConfig;
		private CameraSettingsSection _cameraSettingsConfig;
		private List<PresetSettings> _presets = new List<PresetSettings>();
		private bool _arePresetsDirty = false;

		// Camera Slider
		private string _selectedDeviceId = "";
		private CameraSliderDeviceWatcher _pairedWatcher;
		private CameraSliderDevice _cameraSliderDevice;
		private bool _deviceReconnectionPending = false;
		private bool _deviceCalibrationRunning = false;

		// Twitch Chat Client
		private ITwitchAPI _twitchAPI;
		private TwitchClient _twitchClient;
		private bool _twitchClientIsConnected = false;
		private bool _twitchClientReconnectionPending = false;

		// Twitch PubSub Client
		private TwitchPubSub _twitchPubsub;
		private bool _twitchPubSubIsConnected = false;
		private bool _twitchPubSubReconnectionPending = false;

		// OBS Web Socket
		private bool _obsReconnectionPending = false;
		private OBSWebsocket _obsClient;

		// Watchdog timers
		private Timer _deviceWatchdogTimer = null;
		private static int _keepAliveDelay = 10;
		private int _keepAliveCountdown = _keepAliveDelay;
		private Timer _twitchPubSubWatchdogTimer = null;
		private Timer _twitchClientWatchdogTimer = null;
		private Timer _obsWatchdogTimer = null;

		// Preset state
		private Timer _presetStatusPollTimer = null;
		private float _presetTargetSlidePosition = 0.0f;
		private float _presetTargetPanPosition = 0.0f;
		private float _presetTargetTiltPosition = 0.0f;
		private string _presetBackupObsScene = "";

		public MainWindow()
		{
			InitializeComponent();

			_selectedDeviceId = "";
			_pairedWatcher = new CameraSliderDeviceWatcher(DeviceSelector.BluetoothLePairedOnly);
			_pairedWatcher.DeviceAdded += OnPaired_DeviceAdded;
			_pairedWatcher.DeviceRemoved += OnPaired_DeviceRemoved;
			_pairedWatcher.Start();

			// Register Twitch PubSub to get channel point redeems
			_twitchAPI = new TwitchAPI();
			_twitchPubsub = new TwitchPubSub();
			_twitchPubsub.OnPubSubServiceConnected += OnTwitchPubsubConnected;
			_twitchPubsub.OnPubSubServiceClosed += OnTwitchPubsubClosed;
			_twitchPubsub.OnPubSubServiceError += OnTwitchPubsubError;

			// Register Twitch Client to get chat events
			var clientOptions = new ClientOptions
			{
				MessagesAllowedInPeriod = 750,
				ThrottlingPeriod = TimeSpan.FromSeconds(30)
			};
			WebSocketClient webSocketClient = new WebSocketClient(clientOptions);
			_twitchClient = new TwitchClient(webSocketClient);
			_twitchClient.OnConnected += OnTwitchClientConnected;
			_twitchClient.OnMessageReceived += OnTwitchClientMessageReceived;
			_twitchClient.OnDisconnected += OnTwitchClientDisconnected;

			// Register to OBS Studio websocket API to control OBS Scene remotely
			_obsClient = new OBSWebsocket();
			_obsClient.Connected += OnObsConnected;
			_obsClient.OBSExit += OnObsExited;
			_obsClient.Disconnected += OnObsDisconnected;

			// Register to Bluetooth LE Camera Slider Device Manager
			_cameraSliderDevice = new CameraSliderDevice();
			_cameraSliderDevice.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;
			_cameraSliderDevice.CameraSliderEventHandler += CameraSliderEventReceived;

			// Load the config file and update the UI based on saved settings
			SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, true);
			LoadConfig();
			ApplyConfigToUI();

			// Timers used to monitor the state of the various connections
			_deviceWatchdogTimer = new Timer(DeviceWatchdogCallback, null, 0, 1000);
			_twitchPubSubWatchdogTimer = new Timer(TwitchPubSubWatchdogCallback, null, 0, 1000);
			_twitchClientWatchdogTimer = new Timer(TwitchClientWatchdogCallback, null, 0, 1000);
			_obsWatchdogTimer = new Timer(ObsStudioWatchdogCallback, null, 0, 1000);
		}

		protected async override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			_pairedWatcher.Stop();

			if (_cameraSliderDevice.IsConnected)
			{
				await _cameraSliderDevice.DisconnectAsync();
			}

			_twitchClient.Disconnect();
			_twitchPubsub.Disconnect();
			_obsClient.Disconnect();
		}

		// Config
		protected void LoadConfig()
		{
			// Open App.Config of executable
			_configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

			_twitchWebApiConfig = (TwitchWebAPISection)_configFile.GetSection("twitch_webapi_settings");
			if (_twitchWebApiConfig == null)
			{
				_twitchWebApiConfig = new TwitchWebAPISection();
				_configFile.Sections.Add("twitch_webapi_settings", _twitchWebApiConfig);
			}

			_obsConfig = (ObsConfigSection)_configFile.GetSection("obs_socket_settings");
			if (_obsConfig == null)
			{
				_obsConfig = new ObsConfigSection();
				_configFile.Sections.Add("obs_socket_settings", _obsConfig);
			}

			_presets = new List<PresetSettings>();
			_arePresetsDirty= false;
			_cameraSettingsConfig = (CameraSettingsSection)_configFile.GetSection("camera_settings");
			if (_cameraSettingsConfig != null)
			{
				if (_cameraSettingsConfig.PresetJson != "")
				{
					_presets= JsonConvert.DeserializeObject<List<PresetSettings>>(_cameraSettingsConfig.PresetJson);
				}
			}
			else
			{
				_cameraSettingsConfig = new CameraSettingsSection();
				_configFile.Sections.Add("camera_settings", _cameraSettingsConfig);
			}

			_configFile.Save();
		}

		protected void SaveConfig()
		{
			if (_arePresetsDirty)
			{
				_cameraSettingsConfig.PresetJson = JsonConvert.SerializeObject(_presets, Formatting.None);
				_arePresetsDirty = false;
			}
			_configFile.Save();
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
			d("Received slider event received: " + arg.Message);

			switch(arg.Message)
			{
			case "calibration_started":
				{
					_deviceCalibrationRunning = true;
					SetUIControlsDisableFlag(UIControlDisableFlags.Calibrating, true);
					SetCameraStatusLabel ("Calibrating...");
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

		private void OnDeviceConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
		{
			bool connected = args.IsConnected;
			d("Current connection status is: " + connected);

			if (connected)
			{
				SetCameraStatusLabel("Connected");
				SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, false);
			}
			else
			{
				SetCameraStatusLabel("Searching...");
				SetActivePresetStatusLabel("");
				SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, true);
			}
		}

		private void OnPaired_DeviceAdded(object sender, CameraSlider.Bluetooth.Events.DeviceAddedEventArgs e)
		{
			Debug.WriteLine("Paired Device Added: " + e.Device.Id);

			if (e.Device.Name == "Camera Slider" && _selectedDeviceId == "")
			{
				_selectedDeviceId = e.Device.Id;
			}
		}

		private void OnPaired_DeviceRemoved(object sender, CameraSlider.Bluetooth.Events.DeviceRemovedEventArgs e)
		{
			Debug.WriteLine("Paired device Removed: " + e.Device.Id);

			if (_selectedDeviceId == e.Device.Id)
			{
				_selectedDeviceId = "";
			}
		}

		private async void DeviceWatchdogCallback(Object context)
		{
			// Ping a connected device regularly to keep it awake
			if (_cameraSliderDevice.IsConnected)
			{
				_keepAliveCountdown--;
				if (_keepAliveCountdown <= 0)
				{
					await _cameraSliderDevice.WakeUp();
					_keepAliveCountdown = _keepAliveDelay;
				}
			}
			// Attempt to kick off an async device reconnection
			else if (_selectedDeviceId != "" && !_deviceReconnectionPending && !_cameraSliderDevice.IsConnected)
			{
				_deviceReconnectionPending = true;
				try
				{
					await _cameraSliderDevice.ConnectAsync(_selectedDeviceId);
				}
				catch (Exception ex)
				{
					d("Device connect error: " + ex.Message);
				}
				_deviceReconnectionPending = false;

				// Clear the control UI disable if the device is connected
				if (_cameraSliderDevice.IsConnected)
				{
					SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, false);
				}
			}
		}

		private async void ActivatePreset(PresetSettings preset)
		{
			_presetTargetSlidePosition = preset.SlidePosition;
			_presetTargetPanPosition = preset.PanPosition;
			_presetTargetTiltPosition = preset.TiltPosition;
	
			SetActivePresetStatusLabel("Moving To "+preset.PresetName);
			await _cameraSliderDevice.SetSlidePosition(_presetTargetSlidePosition);
			await _cameraSliderDevice.SetPanPosition(_presetTargetPanPosition);
			await _cameraSliderDevice.SetTiltPosition(_presetTargetTiltPosition);

			await RunOnUiThread(() =>
			{
				SlidePosSlider.Value = preset.SlidePosition;
				PanPosSlider.Value = preset.PanPosition;
				TiltPosSlider.Value = preset.TiltPosition;
			});

			if (_presetStatusPollTimer != null)
			{
				_presetStatusPollTimer.Dispose();
				_presetStatusPollTimer= null;
			}

			if (preset.ObsScene.Length > 0)
			{
				// If there wasn't already a backup OBS scene, save the current scene
				if (_presetBackupObsScene.Length == 0)
				{
					_presetBackupObsScene = GetObsSceneName();
				}

				// Switch to the new OBS scene
				SetObsSceneByName(preset.ObsScene);
			}

			// Start a polling timer to see if the camera reached the target position
			_presetStatusPollTimer = new Timer(PresetStatusPollCallback, preset, 0, 100);
		}

		private async void PresetStatusPollCallback(object state)
		{
			const float kEpsilon= 0.01f;
			
			float slidePos= await _cameraSliderDevice.GetSlidePosition();
			float panPos= await _cameraSliderDevice.GetPanPosition();
			float tiltPos= await _cameraSliderDevice.GetTiltPosition();

			if (Math.Abs(slidePos - _presetTargetSlidePosition) < kEpsilon &&
				Math.Abs(panPos - _presetTargetPanPosition) < kEpsilon &&
				Math.Abs(tiltPos - _presetTargetTiltPosition) < kEpsilon)
			{
				// Halt the timer
				_presetStatusPollTimer.Change(Timeout.Infinite, Timeout.Infinite);

				// Restore back to the previous OBS scene
				if (_presetBackupObsScene.Length > 0)
				{
					SetObsSceneByName(_presetBackupObsScene);
					_presetBackupObsScene= "";
				}

				SetActivePresetStatusLabel("");
			}
		}

		private async void SetActivePresetStatusLabel(string status)
		{
			await RunOnUiThread(() =>
			{
				ActivePresetTxtStatus.Content = status;
			});
		}

		// Twitch PubSub Event Handlers
		private async void TwitchPubSubWatchdogCallback(Object context)
		{
			// Attempt reconnection to stream labs
			if (_twitchWebApiConfig.ChannelID != "" &&
				_twitchWebApiConfig.ClientID != "" &&
				_twitchWebApiConfig.Secret != "" &&
				!_twitchPubSubReconnectionPending &&
				!_twitchPubSubIsConnected)
			{
				await RunOnUiThread(() =>
				{
					TwitchPubSubTxtStatus.Content = "Connecting...";
				});

				_twitchPubSubReconnectionPending = true;

				_twitchAPI.Settings.ClientId = _twitchWebApiConfig.ClientID;
				_twitchAPI.Settings.Secret = _twitchWebApiConfig.Secret;
				_twitchPubsub.ListenToChannelPoints(_twitchWebApiConfig.ChannelID);
				_twitchPubsub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
				_twitchPubsub.Connect();
			}
		}

		private void OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
		{
			var reward = e.RewardRedeemed.Redemption.Reward;
			string rewardId = reward.Id;

			foreach (PresetSettings preset in _presets)
			{
				if (preset.RedeemTrigger.IsActive &&
					preset.RedeemTrigger.TriggerName == rewardId)
				{
					ActivatePreset(preset);
					break;
				}
			}
		}

		private async void OnTwitchPubsubError(object sender, OnPubSubServiceErrorArgs e)
		{
			if (_twitchPubSubReconnectionPending)
			{
				await RunOnUiThread(() =>
				{
					d("Twitch PubSub failed to connect");
					TwitchPubSubTxtStatus.Content = "Disconnected";
				});

				_twitchPubSubReconnectionPending = false;
			}
		}

		private async void OnTwitchPubsubClosed(object sender, EventArgs e)
		{
			await RunOnUiThread(() =>
			{
				d("Twitch PubSub disconnected");
				TwitchPubSubTxtStatus.Content = "Disconnected";
			});

			_twitchPubSubIsConnected = true;
			_twitchPubSubReconnectionPending = false;
		}

		private async void OnTwitchPubsubConnected(object sender, EventArgs e)
		{
			await RunOnUiThread(() =>
			{
				d("Twitch PubSub connected");
				TwitchPubSubTxtStatus.Content = "Connected";
			});

			_twitchPubSubIsConnected = true;
		}

		// Twitch Client Event Handlers
		private void TwitchClientWatchdogCallback(Object context)
		{
			// Attempt reconnection to stream labs
			if (_twitchWebApiConfig.ChannelID != "" &&
				_twitchWebApiConfig.ClientID != "" &&
				_twitchWebApiConfig.Secret != "" &&
				!_twitchClientIsConnected &&
				!_twitchClientReconnectionPending)
			{
				_twitchClientReconnectionPending = true;

				ConnectionCredentials credentials = new ConnectionCredentials(_twitchWebApiConfig.ChannelID, _twitchWebApiConfig.Secret);
				_twitchClient.Initialize(credentials, _twitchWebApiConfig.ChannelID);
				_twitchClient.Connect();
			}
		}

		private async void OnTwitchClientDisconnected(object sender, OnDisconnectedEventArgs e)
		{
			await RunOnUiThread(() =>
			{
				d("Twitch Client disconnected");
				TwitchClientTxtStatus.Content = "Disconnected";
			});

			_twitchClientIsConnected = false;
			_twitchClientReconnectionPending= false;
		}

		private async void OnTwitchClientConnected(object sender, OnConnectedArgs e)
		{
			await RunOnUiThread(() =>
			{
				d("Twitch Client connected");
				TwitchClientTxtStatus.Content = "Connected";
			});

			_twitchClientIsConnected = true;
			_twitchClientReconnectionPending = false;
		}

		private void OnTwitchClientMessageReceived(object sender, OnMessageReceivedArgs e)
		{
			foreach (PresetSettings preset in _presets)
			{
				if (preset.ChatTrigger.IsActive && 
					preset.ChatTrigger.TriggerName == e.ChatMessage.Message &&
					(!preset.ChatTrigger.IsModOnly || e.ChatMessage.IsModerator))
				{
					ActivatePreset(preset);
					break;
				}
			}
		}

		// OBS Studio Event Handlers
		private async void ObsStudioWatchdogCallback(Object context)
		{
			if (_obsConfig.SocketAddress != "")
			{
				if (_obsReconnectionPending)
				{
					if (_obsClient.IsConnected)
					{
						OnObsConnected(this, new EventArgs { });
					}
				}
				else if (!_obsClient.IsConnected)
				{
					await RunOnUiThread(() =>
					{
						ObsTxtStatus.Content = "Connecting...";
					});

					_obsReconnectionPending = true;
					_obsClient.Connect("ws://" + _obsConfig.SocketAddress, _obsConfig.Password);
				}
			}
			else
			{
				if (_obsClient.IsConnected || _obsReconnectionPending)
				{
					_obsReconnectionPending = false;
					_obsClient.Disconnect();
				}
			}
		}

		protected string GetObsSceneName()
		{
			if (_obsClient == null || !_obsClient.IsConnected)
				return "";

			try
			{
				return _obsClient.GetCurrentScene().Name;
			}
			catch (System.InvalidOperationException)
			{
				return "";
			}
		}

		protected bool SetObsSceneByName(string SceneName)
		{
			if (_obsClient == null || !_obsClient.IsConnected)
				return false;

			try
			{
				_obsClient.SetCurrentScene(SceneName);

				return GetObsSceneName() == SceneName;
			}
			catch (System.InvalidOperationException)
			{
				return false;
			}
		}

		protected async Task SetObsSceneForDuration(string NewSceneName, int Milliseconds)
		{
			if (NewSceneName != "")
			{
				string OrigSceneName = GetObsSceneName();
				if (OrigSceneName != NewSceneName)
				{
					bool bSuccess = SetObsSceneByName(NewSceneName);
					if (bSuccess)
					{
						await Task.Delay(Milliseconds);
						SetObsSceneByName(OrigSceneName);
					}
				}
			}
		}

		protected async void OnObsConnected(object sender, EventArgs e)
		{
			await RunOnUiThread(() =>
			{
				d("OBS Studio connected");
				ObsTxtStatus.Content = "Connected";
			});

			_obsReconnectionPending = false;
		}

		protected async void OnObsExited(object sender, EventArgs e)
		{
			await RunOnUiThread(() =>
			{
				d("OBS Studio Exited");
				ObsTxtStatus.Content = "Exited";
			});

			_obsReconnectionPending = false;
		}

		protected async void OnObsDisconnected(object sender, EventArgs e)
		{
			if (!_obsReconnectionPending)
				return;

			await RunOnUiThread(() =>
			{
				d("OBS Studio disconnected");
				ObsTxtStatus.Content = "Disconnected";
			});

			_obsReconnectionPending = false;
		}

		// UI Functions
		protected void ApplyConfigToUI()
		{
			SlidePosSlider.Value = _cameraSettingsConfig.SlidePos;
			SlideSpeedSlider.Value = _cameraSettingsConfig.SlideSpeed;
			SlideAccelSlider.Value = _cameraSettingsConfig.SlideAccel;
			PanPosSlider.Value = _cameraSettingsConfig.PanPos;
			PanSpeedSlider.Value = _cameraSettingsConfig.PanSpeed;
			PanAccelSlider.Value = _cameraSettingsConfig.PanAccel;
			TiltPosSlider.Value = _cameraSettingsConfig.TiltPos;
			TiltSpeedSlider.Value = _cameraSettingsConfig.TiltSpeed;
			TiltAccelSlider.Value = _cameraSettingsConfig.TiltAccel;

			SlidePosStatus.Content = _cameraSettingsConfig.SlidePos.ToString("0.00");
			SlideSpeedStatus.Content = _cameraSettingsConfig.SlideSpeed.ToString("0.00");
			SlideAccelStatus.Content = _cameraSettingsConfig.SlideAccel.ToString("0.00");
			PanPosStatus.Content = _cameraSettingsConfig.PanPos.ToString("0.00");
			PanSpeedStatus.Content = _cameraSettingsConfig.PanSpeed.ToString("0.00");
			PanAccelStatus.Content = _cameraSettingsConfig.PanAccel.ToString("0.00");
			TiltPosStatus.Content = _cameraSettingsConfig.TiltPos.ToString("0.00");
			TiltSpeedStatus.Content = _cameraSettingsConfig.TiltSpeed.ToString("0.00");
			TiltAccelStatus.Content = _cameraSettingsConfig.TiltAccel.ToString("0.00");

			RebuildPresetComboBox();

			// Apply the config settings to the settings tab
			TwitchClientIdInput.Text = _twitchWebApiConfig.ClientID;
			TwitchChannelIdInput.Text = _twitchWebApiConfig.ChannelID;
			TwitchSecretKeyInput.Password = _twitchWebApiConfig.Secret;

			SocketAddressInput.Text = _obsConfig.SocketAddress;
			SocketPasswordInput.Password = _obsConfig.Password;
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

		private void TwitchChannelIdInput_Changed(object sender, RoutedEventArgs e)
		{
			_twitchWebApiConfig.ChannelID = TwitchClientIdInput.Text;
			SaveConfig();

			_twitchPubsub.Disconnect();
		}

		private void TwitchClientIdInput_Changed(object sender, RoutedEventArgs e)
		{
			_twitchWebApiConfig.ClientID = TwitchClientIdInput.Text;
			SaveConfig();

			_twitchPubsub.Disconnect();
		}

		private void TwitchSecretInput_Changed(object sender, RoutedEventArgs e)
		{
			_twitchWebApiConfig.Secret = TwitchSecretKeyInput.Password;
			SaveConfig();

			_twitchPubsub.Disconnect();
		}

		private void NumericalInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			Regex regex = new Regex("[^0-9]+");
			e.Handled = regex.IsMatch(e.Text);
		}

		private void SocketAddressInput_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (_obsConfig.SocketAddress != SocketAddressInput.Text)
			{
				_obsConfig.SocketAddress = SocketAddressInput.Text;
				SaveConfig();

				if (_obsClient.IsConnected)
				{
					_obsClient.Disconnect();
				}
			}
		}

		private void SocketPasswordInput_TextChanged(object sender, RoutedEventArgs e)
		{
			if (_obsConfig.Password != SocketPasswordInput.Password)
			{
				_obsConfig.Password = SocketPasswordInput.Password;
				SaveConfig();

				if (_obsClient.IsConnected)
				{
					_obsClient.Disconnect();
				}
			}
		}

		private async void SlidePos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.SlidePos = (int)SlidePosSlider.Value;
			await _cameraSliderDevice.SetSlidePosition(_cameraSettingsConfig.SlidePos);
			SlidePosStatus.Content = _cameraSettingsConfig.SlidePos.ToString("0.00");
		}

		private async void PanPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.PanPos = (int)PanPosSlider.Value;
			await _cameraSliderDevice.SetSlidePosition(_cameraSettingsConfig.PanPos);
			PanPosStatus.Content = _cameraSettingsConfig.PanPos.ToString("0.00");
		}

		private async void TiltPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.TiltPos = (int)TiltPosSlider.Value;
			await _cameraSliderDevice.SetSlidePosition(_cameraSettingsConfig.TiltPos);
			TiltPosStatus.Content = _cameraSettingsConfig.TiltPos.ToString("0.00");
		}

		private async void SlideSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.SlideSpeed = (int)SlideSpeedSlider.Value;
			await _cameraSliderDevice.SetSlideSpeed(_cameraSettingsConfig.SlideSpeed);
			SlideSpeedStatus.Content = _cameraSettingsConfig.SlideSpeed.ToString("0.00");
		}

		private async void PanSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.PanSpeed = (int)PanSpeedSlider.Value;
			await _cameraSliderDevice.SetSlideSpeed(_cameraSettingsConfig.PanSpeed);
			PanSpeedStatus.Content = _cameraSettingsConfig.PanSpeed.ToString("0.00");
		}

		private async void TiltSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.TiltSpeed = (int)TiltSpeedSlider.Value;
			await _cameraSliderDevice.SetSlideSpeed(_cameraSettingsConfig.TiltSpeed);
			TiltSpeedStatus.Content = _cameraSettingsConfig.TiltSpeed.ToString("0.00");
		}

		private async void SlideAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.SlideAccel = (int)SlideAccelSlider.Value;
			await _cameraSliderDevice.SetSlideAcceleration(_cameraSettingsConfig.SlideAccel);
			SlideAccelStatus.Content = _cameraSettingsConfig.SlideAccel.ToString("0.00");
		}

		private async void PanAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.PanAccel = (int)PanAccelSlider.Value;
			await _cameraSliderDevice.SetSlideAcceleration(_cameraSettingsConfig.PanAccel);
			PanAccelStatus.Content = _cameraSettingsConfig.PanAccel.ToString("0.00");
		}

		private async void TiltAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			_cameraSettingsConfig.TiltAccel = (int)TiltAccelSlider.Value;
			await _cameraSliderDevice.SetSlideAcceleration(_cameraSettingsConfig.TiltAccel);
			TiltAccelStatus.Content = _cameraSettingsConfig.TiltAccel.ToString("0.00");
		}

		private void BtnGotoPreset_Click(object sender, RoutedEventArgs e)
		{
			if (PresetComboBox.SelectedIndex != -1)
			{
				ActivatePreset(_presets[PresetComboBox.SelectedIndex]);
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
				EditPresetWindow editPresetWindow = new EditPresetWindow(_cameraSettingsConfig, _presets, index);
				editPresetWindow.Owner = this;
				editPresetWindow.Show();
				editPresetWindow.Closed += EditPresetWindow_Closed;
			}
		}

		private void BtnAddPreset_Click(object sender, RoutedEventArgs e)
		{
			EditPresetWindow editPresetWindow = new EditPresetWindow(_cameraSettingsConfig, _presets, -1);
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
				_presets.RemoveAt(index);
				PresetComboBox.Items.RemoveAt(index);
			}
		}

		private void RebuildPresetComboBox()
		{
			PresetComboBox.Items.Clear();
			foreach (PresetSettings preset in _presets)
			{
				PresetComboBox.Items.Add(preset.PresetName);
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

		[Conditional("DEBUG")]
		private void d(string txt)
		{
			Debug.WriteLine(txt);
		}
	}
}
