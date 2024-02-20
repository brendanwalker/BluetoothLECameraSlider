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
      IsEnabled = false;
      PatternSceneName = "Main";
      PatternSceneDuration = 3000;
      SocketAddress = "127.0.0.1:4444";
      Password = "";
    }

    [ConfigurationProperty("is_enabled")]
    public bool IsEnabled
    {
      get
      {
        return (bool)this["is_enabled"];
      }
      set
      {
        this["is_enabled"] = value;
      }
    }

    [ConfigurationProperty("pattern_scene_name")]
    public string PatternSceneName
    {
      get
      {
        return (string)this["pattern_scene_name"];
      }
      set
      {
        this["pattern_scene_name"] = value;
      }
    }

    [ConfigurationProperty("pattern_scene_duration")]
    public int PatternSceneDuration
    {
      get
      {
        return (int)this["pattern_scene_duration"];
      }
      set
      {
        this["pattern_scene_duration"] = value;
      }
    }

    [ConfigurationProperty("socket_address")]
    public string SocketAddress
    {
      get
      {
        return (string)this["socket_address"];
      }
      set
      {
        this["socket_address"] = value;
      }
    }

    [ConfigurationProperty("password")]
    public string Password
    {
      get
      {
        return (string)this["password"];
      }
      set
      {
        this["password"] = value;
      }
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
      get
      {
        return (string)this["channel_id"];
      }
      set
      {
        this["channel_id"] = value;
      }
    }

    [ConfigurationProperty("client_id")]
    public string ClientID
    {
      get
      {
        return (string)this["client_id"];
      }
      set
      {
        this["client_id"] = value;
      }
    }

    [ConfigurationProperty("secret")]
    public string Secret
    {
      get
      {
        return (string)this["secret"];
      }
      set
      {
        this["secret"] = value;
      }
    }
  }

  public class TriggerSettings
  {
    public string TriggerName
    {
      get; set;
    }
    public bool IsModOnly
    {
      get; set;
    }
    public bool IsActive
    {
      get; set;
    }

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
    public string PresetName
    {
      get; set;
    }
    public float SlidePosition
    {
      get; set;
    }
    public float PanPosition
    {
      get; set;
    }
    public float TiltPosition
    {
      get; set;
    }
    public String ObsScene
    {
      get; set;
    }
    public TriggerSettings ChatTrigger
    {
      get; set;
    }
    public TriggerSettings RedeemTrigger
    {
      get; set;
    }

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
      get
      {
        return (float)this["slide_pos"];
      }
      set
      {
        this["slide_pos"] = value;
      }
    }

    [ConfigurationProperty("slide_speed")]
    public float SlideSpeed
    {
      get
      {
        return (float)this["slide_speed"];
      }
      set
      {
        this["slide_speed"] = value;
      }
    }

    [ConfigurationProperty("slide_accel")]
    public float SlideAccel
    {
      get
      {
        return (float)this["slide_accel"];
      }
      set
      {
        this["slide_accel"] = value;
      }
    }

    [ConfigurationProperty("pan_pos")]
    public float PanPos
    {
      get
      {
        return (float)this["pan_pos"];
      }
      set
      {
        this["pan_pos"] = value;
      }
    }

    [ConfigurationProperty("pan_speed")]
    public float PanSpeed
    {
      get
      {
        return (float)this["pan_speed"];
      }
      set
      {
        this["pan_speed"] = value;
      }
    }

    [ConfigurationProperty("pan_accel")]
    public float PanAccel
    {
      get
      {
        return (float)this["pan_accel"];
      }
      set
      {
        this["pan_accel"] = value;
      }
    }

    [ConfigurationProperty("tilt_pos")]
    public float TiltPos
    {
      get
      {
        return (float)this["tilt_pos"];
      }
      set
      {
        this["tilt_pos"] = value;
      }
    }

    [ConfigurationProperty("tilt_speed")]
    public float TiltSpeed
    {
      get
      {
        return (float)this["tilt_speed"];
      }
      set
      {
        this["tilt_speed"] = value;
      }
    }

    [ConfigurationProperty("tilt_accel")]
    public float TiltAccel
    {
      get
      {
        return (float)this["tilt_accel"];
      }
      set
      {
        this["tilt_accel"] = value;
      }
    }

    [ConfigurationProperty("preset_json")]
    public string PresetJson
    {
      get
      {
        return (string)this["preset_json"];
      }
      set
      {
        this["preset_json"] = value;
      }
    }
  }

  public class ConfigState
  {
    public Configuration _configFile;
    public TwitchWebAPISection _twitchWebApiConfig;
    public ObsConfigSection _obsConfig;
    public CameraSettingsSection _cameraSettingsConfig;
    public bool _areCameraSettingsDirty = false;
    public List<PresetSettings> _presets = new List<PresetSettings>();
    public bool _arePresetsDirty = false;

    public void LoadConfig()
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
      _arePresetsDirty = false;
      _cameraSettingsConfig = (CameraSettingsSection)_configFile.GetSection("camera_settings");
      if (_cameraSettingsConfig != null)
      {
        if (_cameraSettingsConfig.PresetJson != "")
        {
          _presets = JsonConvert.DeserializeObject<List<PresetSettings>>(_cameraSettingsConfig.PresetJson);
        }
      }
      else
      {
        _cameraSettingsConfig = new CameraSettingsSection();
        _configFile.Sections.Add("camera_settings", _cameraSettingsConfig);
      }

      _configFile.Save();
    }

    public void SaveConfig()
    {
      if (_arePresetsDirty)
      {
        _cameraSettingsConfig.PresetJson = JsonConvert.SerializeObject(_presets, Formatting.None);
        _arePresetsDirty = false;
      }
      _areCameraSettingsDirty = false;
      _configFile.Save();
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
    private ConfigState _configState = new ConfigState();

    // Camera Slider
    CancellationTokenSource _deviceWatchdogCancelSignaler = new CancellationTokenSource();
    private string _deviceName = "Camera Slider";
    private CameraSliderDevice _cameraSliderDevice;
    private bool _deviceCalibrationRunning = false;
    private static int _deviceKeepAliveDelay = 10;
    private int _deviceKeepAliveCountdown = _deviceKeepAliveDelay;
    private bool _suppressUIUpdatesToDevice = false;

    // Twitch Chat Client
    CancellationTokenSource _twitchApiWatchdogCancelSignaler = new CancellationTokenSource();
    private ITwitchAPI _twitchAPI;
    private TwitchClient _twitchClient;
    private bool _twitchClientIsConnected = false;

    // Twitch PubSub Client
    CancellationTokenSource _twitchPubSubWatchdogCancelSignaler = new CancellationTokenSource();
    private TwitchPubSub _twitchPubsub;
    private bool _twitchPubSubIsConnected = false;

    // OBS Web Socket
    CancellationTokenSource _obsClientCancelSignaler = new CancellationTokenSource();
    private OBSWebsocket _obsClient;

    // Preset state
    CancellationTokenSource _presetCancelSignaler = null;
    private float _presetTargetSlidePosition = 0.0f;
    private float _presetTargetPanPosition = 0.0f;
    private float _presetTargetTiltPosition = 0.0f;
    private string _presetBackupObsScene = "";
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
      _configState.LoadConfig();
      ApplyConfigToUI();

      // Kick off the watchdog workers
      _ = StartDeviceWatchdogWorker(_deviceWatchdogCancelSignaler.Token);
      _ = StartTwitchClientWatchdogWorker(_twitchApiWatchdogCancelSignaler.Token);
      _ = StartTwitchPubSubWatchdogWorker(_twitchPubSubWatchdogCancelSignaler.Token);

      if (_configState._obsConfig.IsEnabled)
      {
        _ = StartObsStudioWatchdogWorker(_obsClientCancelSignaler.Token);
      }
    }

    protected async override void OnClosing(CancelEventArgs e)
    {
      base.OnClosing(e);

      // Stop any running async tasks
      _deviceWatchdogCancelSignaler.Cancel();
      _twitchApiWatchdogCancelSignaler.Cancel();
      _twitchPubSubWatchdogCancelSignaler.Cancel();
      _obsClientCancelSignaler.Cancel();
      if (_presetCancelSignaler != null)
      {
        _presetCancelSignaler.Cancel();
      }

      // Disconnect from everything
      if (_cameraSliderDevice.IsConnected)
      {
        await _cameraSliderDevice.DisconnectAsync();
      }
      _twitchClient.Disconnect();
      _twitchPubsub.Disconnect();
      _obsClient.Disconnect();
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
      d("Current connection status is: " + connected);

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
              d("Device connect error: " + ex.Message);
            }

            // Clear the control UI disable if the device is connected
            if (_cameraSliderDevice.IsConnected)
            {
              SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, false);
            }
          }

          if (_configState._areCameraSettingsDirty)
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

      // Switch to the desired scene
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

      // Wait for the camera to reach the target position
      try
      {
        while (_hasPendingPresetTarget)
        {
          await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
      }
      catch (OperationCanceledException)
      {
        // Handle the cancellation
        Console.WriteLine("Preset was cancelled.");
      }

      // Restore back to the previous OBS scene
      if (_presetBackupObsScene.Length > 0)
      {
        SetObsSceneByName(_presetBackupObsScene);
        _presetBackupObsScene = "";
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

    // Twitch PubSub Event Handlers
    private async Task StartTwitchPubSubWatchdogWorker(CancellationToken cancellationToken)
    {
      try
      {
        while (true)
        {
          // Attempt reconnection to stream labs
          if (_configState._twitchWebApiConfig.ChannelID != "" &&
            _configState._twitchWebApiConfig.ClientID != "" &&
            _configState._twitchWebApiConfig.Secret != "" &&
            !_twitchPubSubIsConnected)
          {
            await RunOnUiThread(() =>
            {
              TwitchPubSubTxtStatus.Content = "Connecting...";
            });

            _twitchAPI.Settings.ClientId = _configState._twitchWebApiConfig.ClientID;
            _twitchAPI.Settings.Secret = _configState._twitchWebApiConfig.Secret;
            _twitchPubsub.ListenToChannelPoints(_configState._twitchWebApiConfig.ChannelID);
            _twitchPubsub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
            _twitchPubsub.Connect();
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

    private void OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
    {
      var reward = e.RewardRedeemed.Redemption.Reward;
      string rewardId = reward.Id;

      foreach (PresetSettings preset in _configState._presets)
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
      await RunOnUiThread(() =>
      {
        d("Twitch PubSub failed to connect");
        TwitchPubSubTxtStatus.Content = "Disconnected";
      });
    }

    private async void OnTwitchPubsubClosed(object sender, EventArgs e)
    {
      await RunOnUiThread(() =>
      {
        d("Twitch PubSub disconnected");
        TwitchPubSubTxtStatus.Content = "Disconnected";
      });

      _twitchPubSubIsConnected = true;
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
    private async Task StartTwitchClientWatchdogWorker(CancellationToken cancellationToken)
    {
      try
      {
        while (true)
        {
          // Attempt reconnection to stream labs
          if (_configState._twitchWebApiConfig.ChannelID != "" &&
            _configState._twitchWebApiConfig.ClientID != "" &&
            _configState._twitchWebApiConfig.Secret != "" &&
            !_twitchClientIsConnected)
          {
            ConnectionCredentials credentials =
              new ConnectionCredentials(
                _configState._twitchWebApiConfig.ChannelID,
                _configState._twitchWebApiConfig.Secret);
            _twitchClient.Initialize(credentials, _configState._twitchWebApiConfig.ChannelID);
            _twitchClient.Connect();
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

    private async void OnTwitchClientDisconnected(object sender, OnDisconnectedEventArgs e)
    {
      await RunOnUiThread(() =>
      {
        d("Twitch Client disconnected");
        TwitchClientTxtStatus.Content = "Disconnected";
      });

      _twitchClientIsConnected = false;
    }

    private async void OnTwitchClientConnected(object sender, OnConnectedArgs e)
    {
      await RunOnUiThread(() =>
      {
        d("Twitch Client connected");
        TwitchClientTxtStatus.Content = "Connected";
      });

      _twitchClientIsConnected = true;
    }

    private void OnTwitchClientMessageReceived(object sender, OnMessageReceivedArgs e)
    {
      foreach (PresetSettings preset in _configState._presets)
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
    private void ObsCheckBox_Checked(object sender, RoutedEventArgs e)
    {
      bool WantEnabled = (bool)ObsCheckBox.IsChecked;

      if (!_configState._obsConfig.IsEnabled && WantEnabled)
      {
        _configState._obsConfig.IsEnabled = true;
        _ = StartObsStudioWatchdogWorker(_obsClientCancelSignaler.Token);
      }
      else if (_configState._obsConfig.IsEnabled && !WantEnabled)
      {
        _configState._obsConfig.IsEnabled = false;
        _obsClientCancelSignaler.Cancel();
        if (_obsClient.IsConnected)
          _obsClient.Disconnect();
      }
    }

    private async Task StartObsStudioWatchdogWorker(CancellationToken cancellationToken)
    {
      try
      {
        while (true)
        {
          if (!_obsClient.IsConnected)
          {
            if (_configState._obsConfig.SocketAddress != "")
            {
              await RunOnUiThread(() =>
              {
                ObsTxtStatus.Content = "Connecting...";
              });

              _obsClient.Connect("ws://" + _configState._obsConfig.SocketAddress, _configState._obsConfig.Password);
            }
          }

          await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
      }
      catch (OperationCanceledException)
      {
        // Handle the cancellation
        Console.WriteLine("Shut down OBS watch dog worker");
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
    }

    protected async void OnObsExited(object sender, EventArgs e)
    {
      await RunOnUiThread(() =>
      {
        d("OBS Studio Exited");
        ObsTxtStatus.Content = "Exited";
      });
    }

    protected async void OnObsDisconnected(object sender, EventArgs e)
    {
      await RunOnUiThread(() =>
      {
        d("OBS Studio disconnected");
        ObsTxtStatus.Content = "Disconnected";
      });
    }

    // UI Functions
    protected void ApplyConfigToUI()
    {
      ObsCheckBox.IsChecked = _configState._obsConfig.IsEnabled;

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

      // Apply the config settings to the settings tab
      TwitchClientIdInput.Text = _configState._twitchWebApiConfig.ClientID;
      TwitchChannelIdInput.Text = _configState._twitchWebApiConfig.ChannelID;
      TwitchSecretKeyInput.Password = _configState._twitchWebApiConfig.Secret;

      SocketAddressInput.Text = _configState._obsConfig.SocketAddress;
      SocketPasswordInput.Password = _configState._obsConfig.Password;
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

    private void TwitchChannelIdInput_Changed(object sender, RoutedEventArgs e)
    {
      _configState._twitchWebApiConfig.ChannelID = TwitchClientIdInput.Text;
      _configState.SaveConfig();

      _twitchPubsub.Disconnect();
    }

    private void TwitchClientIdInput_Changed(object sender, RoutedEventArgs e)
    {
      _configState._twitchWebApiConfig.ClientID = TwitchClientIdInput.Text;
      _configState.SaveConfig();

      _twitchPubsub.Disconnect();
    }

    private void TwitchSecretInput_Changed(object sender, RoutedEventArgs e)
    {
      _configState._twitchWebApiConfig.Secret = TwitchSecretKeyInput.Password;
      _configState.SaveConfig();

      _twitchPubsub.Disconnect();
    }

    private void NumericalInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
      Regex regex = new Regex("[^0-9]+");
      e.Handled = regex.IsMatch(e.Text);
    }

    private void SocketAddressInput_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (_configState._obsConfig.SocketAddress != SocketAddressInput.Text)
      {
        _configState._obsConfig.SocketAddress = SocketAddressInput.Text;
        _configState.SaveConfig();

        if (_obsClient.IsConnected)
        {
          _obsClient.Disconnect();
        }
      }
    }

    private void SocketPasswordInput_TextChanged(object sender, RoutedEventArgs e)
    {
      if (_configState._obsConfig.Password != SocketPasswordInput.Password)
      {
        _configState._obsConfig.Password = SocketPasswordInput.Password;
        _configState.SaveConfig();

        if (_obsClient.IsConnected)
        {
          _obsClient.Disconnect();
        }
      }
    }

    private async void SlidePos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.SlidePos = (float)SlidePosSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isSliderPosDragging)
        await _cameraSliderDevice.SetSlidePosition(_configState._cameraSettingsConfig.SlidePos);
      SlidePosStatus.Content = _configState._cameraSettingsConfig.SlidePos.ToString("0.00");
    }

    private async void PanPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.PanPos = (float)PanPosSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isPanPosDragging)
        await _cameraSliderDevice.SetPanPosition(_configState._cameraSettingsConfig.PanPos);
      PanPosStatus.Content = _configState._cameraSettingsConfig.PanPos.ToString("0.00");
    }

    private async void TiltPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.TiltPos = (float)TiltPosSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isTiltPosDragging)
        await _cameraSliderDevice.SetTiltPosition(_configState._cameraSettingsConfig.TiltPos);
      TiltPosStatus.Content = _configState._cameraSettingsConfig.TiltPos.ToString("0.00");
    }

    private async void SlideSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.SlideSpeed = (float)SlideSpeedSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isSliderSpeedDragging)
        await _cameraSliderDevice.SetSlideSpeed(_configState._cameraSettingsConfig.SlideSpeed);
      SlideSpeedStatus.Content = _configState._cameraSettingsConfig.SlideSpeed.ToString("0.00");
    }

    private async void PanSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.PanSpeed = (float)PanSpeedSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isPanSpeedDragging)
        await _cameraSliderDevice.SetPanSpeed(_configState._cameraSettingsConfig.PanSpeed);
      PanSpeedStatus.Content = _configState._cameraSettingsConfig.PanSpeed.ToString("0.00");
    }

    private async void TiltSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.TiltSpeed = (float)TiltSpeedSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isTiltSpeedDragging)
        await _cameraSliderDevice.SetTiltSpeed(_configState._cameraSettingsConfig.TiltSpeed);
      TiltSpeedStatus.Content = _configState._cameraSettingsConfig.TiltSpeed.ToString("0.00");
    }

    private async void SlideAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.SlideAccel = (float)SlideAccelSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isSliderAccelDragging)
        await _cameraSliderDevice.SetSlideAcceleration(_configState._cameraSettingsConfig.SlideAccel);
      SlideAccelStatus.Content = _configState._cameraSettingsConfig.SlideAccel.ToString("0.00");
    }

    private async void PanAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.PanAccel = (float)PanAccelSlider.Value;
      _configState._areCameraSettingsDirty = true;
      if (!_suppressUIUpdatesToDevice && !_isPanAccelDragging)
        await _cameraSliderDevice.SetPanAcceleration(_configState._cameraSettingsConfig.PanAccel);
      PanAccelStatus.Content = _configState._cameraSettingsConfig.PanAccel.ToString("0.00");
    }

    private async void TiltAccel_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
      _configState._cameraSettingsConfig.TiltAccel = (float)TiltAccelSlider.Value;
      _configState._areCameraSettingsDirty = true;
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
      PresetComboBox.Items.Clear();
      foreach (PresetSettings preset in _configState._presets)
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
