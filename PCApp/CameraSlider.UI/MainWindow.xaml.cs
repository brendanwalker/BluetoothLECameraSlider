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
using System.Threading;
using System.Configuration;
using System.Text.RegularExpressions;
using OBSWebsocketDotNet;
using TwitchLib.PubSub;
using TwitchLib.Api.Interfaces;
using TwitchLib.Api;
using TwitchLib.PubSub.Events;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Client.Events;
using TwitchLib.Communication.Events;
using Newtonsoft.Json;
using System.Net;
using System.IO;
using NHttp;
using System.Linq;

namespace CameraSlider.UI
{
  public class ObsConfigSection : ConfigurationSection
  {
    public ObsConfigSection()
    {
      IsEnabled = false;
      PatternSceneName = "Main";
      PatternSceneDuration = 3000;
      SocketAddress = "127.0.0.1:4455";
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
      ClientSecret = "";
      AccessToken = "";
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

    [ConfigurationProperty("channel_name")]
    public string ChannelName
    {
      get
      {
        return (string)this["channel_name"];
      }
      set
      {
        this["channel_name"] = value;
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

    [ConfigurationProperty("client_secret")]
    public string ClientSecret
    {
      get
      {
        return (string)this["client_secret"];
      }
      set
      {
        this["client_secret"] = value;
      }
    }

    [ConfigurationProperty("access_token")]
    public string AccessToken
    {
      get
      {
        return (string)this["access_token"];
      }
      set
      {
        this["access_token"] = value;
      }
    }

    [ConfigurationProperty("refresh_token")]
    public string RefreshToken
    {
      get
      {
        return (string)this["refresh_token"];
      }
      set
      {
        this["refresh_token"] = value;
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
    public List<PresetSettings> _presets = new List<PresetSettings>();
    public bool _areConfigSettingsDirty = false;
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
      _areConfigSettingsDirty = false;
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
    private ITwitchAPI _twitchAPI;
    private TwitchClient _twitchClient;

    // Twitch PubSub Client
    private TwitchPubSub _twitchPubsub;

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

      // Load the config settings first
      _configState.LoadConfig();

      // Start up local web server to handle Twitch OAuth requests
      InitializeTwitchOAuthWebServer();

      // Initialize the Twitch API
      _twitchAPI = new TwitchAPI();
      _twitchAPI.Settings.ClientId = _configState._twitchWebApiConfig.ClientID;

      // Register to OBS Studio websocket API to control OBS Scene remotely
      _obsClient = new OBSWebsocket();
      _obsClient.Connected += OnObsConnected;
      _obsClient.ExitStarted += OnObsExited;
      _obsClient.Disconnected += OnObsDisconnected;

      // Register to Bluetooth LE Camera Slider Device Manager
      _cameraSliderDevice = new CameraSliderDevice();
      _cameraSliderDevice.ConnectionStatusChanged += OnDeviceConnectionStatusChanged;
      _cameraSliderDevice.CameraSliderEventHandler += CameraSliderEventReceived;

      // Setup the UI
      SetUIControlsDisableFlag(UIControlDisableFlags.DeviceDisconnected, true);
      ApplyConfigToUI();

      // Kick off the watchdog workers
      _ = StartDeviceWatchdogWorker(_deviceWatchdogCancelSignaler.Token);

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

      if (_twitchClient != null)
      {
        _twitchClient.Disconnect();
      }

      if (_twitchPubsub != null)
      {
        _twitchPubsub.Disconnect();
      }

      //_obsClient.Disconnect();

      if (_twitchOAuthWebServer != null)
      {
        _twitchOAuthWebServer.Stop();
        _twitchOAuthWebServer.Dispose();
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
        const float maxWaitDurationSeconds = 15f;
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
    private void OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
    {
      var reward = e.RewardRedeemed.Redemption.Reward;
      string rewardId = reward.Title;

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

    private void OnTwitchPubsubError(object sender, OnPubSubServiceErrorArgs e)
    {
      EmitLog("Twitch PubSub failed to connect");
    }

    private void OnTwitchPubsubClosed(object sender, EventArgs e)
    {
      EmitLog("Twitch PubSub disconnected");
    }

    private void OnTwitchPubsubConnected(object sender, EventArgs e)
    {
      EmitLog("Twitch PubSub connected");
      _twitchPubsub.SendTopics(_configState._twitchWebApiConfig.AccessToken);
    }

    // Twitch Client Event Handlers
    private async void OnTwitchClientDisconnected(object sender, OnDisconnectedEventArgs e)
    {
      EmitLog("Twitch Client disconnected");

      await RunOnUiThread(() =>
      {
        TwitchClientTxtStatus.Content = "Disconnected";
      });
    }

    private async void OnTwitchIncorrectLogin(object sender, OnIncorrectLoginArgs e)
    {
      EmitLog("Twitch Client incorrect login: "+e.Exception.Message);

      // Clear out the access token and refresh token in hopes that re-loggin in will work
      _configState._twitchWebApiConfig.AccessToken = "";
      _configState._twitchWebApiConfig.RefreshToken = "";
      _configState._areConfigSettingsDirty = true;

      await RunOnUiThread(() =>
      {
        TwitchClientTxtStatus.Content = "Login Failed";
      });
    }

    private void OnTwitchConnectionError(object sender, OnConnectionErrorArgs e)
    {
      EmitLog("Twitch Client connection error: "+e.Error);
    }


    private async void OnTwitchClientConnected(object sender, OnConnectedArgs e)
    {
      EmitLog("Twitch Client connected");

      await RunOnUiThread(() =>
      {
        TwitchClientTxtStatus.Content = "Connected";
      });
    }

    private void OnTwitchClientMessageReceived(object sender, OnMessageReceivedArgs e)
    {
      foreach (PresetSettings preset in _configState._presets)
      {
        if (preset.ChatTrigger.IsActive &&
          e.ChatMessage.Message.StartsWith(preset.ChatTrigger.TriggerName) &&
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
      _configState._obsConfig.IsEnabled = true;
      _configState._areConfigSettingsDirty = true;

      _ = StartObsStudioWatchdogWorker(_obsClientCancelSignaler.Token);
    }

    private void ObsCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
      _configState._obsConfig.IsEnabled = false;
      _configState._areConfigSettingsDirty = true;

      _obsClientCancelSignaler.Cancel();
      if (_obsClient.IsConnected)
      {
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

              _obsClient.ConnectAsync("ws://" + _configState._obsConfig.SocketAddress, _configState._obsConfig.Password);
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
        return _obsClient.GetCurrentProgramScene();
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
        _obsClient.SetCurrentProgramScene(SceneName);

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
        EmitLog("OBS Studio connected");
        ObsTxtStatus.Content = "Connected";
      });
    }

    protected async void OnObsExited(object sender, EventArgs e)
    {
      await RunOnUiThread(() =>
      {
        EmitLog("OBS Studio Exited");
        ObsTxtStatus.Content = "Exited";
      });
    }

    protected async void OnObsDisconnected(object sender, OBSWebsocketDotNet.Communication.ObsDisconnectionInfo e)
    {
      await RunOnUiThread(() =>
      {
        EmitLog("OBS Studio disconnected");
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
      TwitchClientIdInput.Password = _configState._twitchWebApiConfig.ClientID;
      TwitchClientSecretKeyInput.Password = _configState._twitchWebApiConfig.ClientSecret;

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

    private void TwitchClientIdInput_Changed(object sender, RoutedEventArgs e)
    {
      if (_configState._twitchWebApiConfig.ClientID != TwitchClientIdInput.Password)
      {
        _configState._twitchWebApiConfig.ClientID = TwitchClientIdInput.Password;
        _configState._areConfigSettingsDirty = true;

        _twitchPubsub.Disconnect();
      }
    }

    private void TwitchClientSecretInput_Changed(object sender, RoutedEventArgs e)
    {
      if (_configState._twitchWebApiConfig.ClientSecret != TwitchClientSecretKeyInput.Password)
      {
        _configState._twitchWebApiConfig.ClientSecret = TwitchClientSecretKeyInput.Password;
        _configState._areConfigSettingsDirty = true;

        _twitchPubsub.Disconnect();
      }
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
        _configState._areConfigSettingsDirty = true;

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
        _configState._areConfigSettingsDirty = true;

        if (_obsClient.IsConnected)
        {
          _obsClient.Disconnect();
        }
      }
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

    // Twitch API Functions
    private HttpServer _twitchOAuthWebServer = null;
    string _twitchRedirectUri = "http://localhost:8080/redirect/";
    List<string> _twitchAccesScopes = new List<string> {
      "chat:read",
      "whispers:read",
      "bits:read",
      "channel:read:subscriptions",
      "channel:read:redemptions"
    };

    private async void BtnTwitchConnect_Click(object sender, RoutedEventArgs e)
    {
      await RunOnUiThread(() =>
      {
        TwitchClientTxtStatus.Content = "Connecting...";
      });

      if (!LaunchTwitchAuthFlow())
      {
        await RunOnUiThread(() =>
        {
          TwitchClientTxtStatus.Content = "Failed";
        });
      }
    }

    bool LaunchTwitchAuthFlow()
    {
      string clientId = _configState._twitchWebApiConfig.ClientID;
      string clientSecret = _configState._twitchWebApiConfig.ClientSecret;
      string accessToken = _configState._twitchWebApiConfig.AccessToken;

      if (clientId != "" && clientSecret != "")
      {
        if (accessToken == "")
        {
          string scopesString = string.Join("+", _twitchAccesScopes);
          string url = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={clientId}&redirect_uri={_twitchRedirectUri}&scope={scopesString}";

          return System.Diagnostics.Process.Start(url) != null;
        }
        else
        {
          return ConnectToTwichAPI();
        }
      }

      return false;
    }

    async Task<bool> FetchAndCacheTwitchAPIConfig(string code)
    {
      bool bSuccess = false;

      try
      {
        var resp =
          await _twitchAPI.Auth.GetAccessTokenFromCodeAsync(
            code,
            _configState._twitchWebApiConfig.ClientSecret,
            _twitchRedirectUri);

        // Store the result on the config state
        _configState._twitchWebApiConfig.AccessToken = resp.AccessToken;
        _configState._twitchWebApiConfig.RefreshToken = resp.RefreshToken;

        // Also copy the access token cached in the config to the Twitch API settings 
        _twitchAPI.Settings.AccessToken = resp.AccessToken;

        // Fetch the authenticated user associated with the access token
        var authenticatedUsers = await _twitchAPI.Helix.Users.GetUsersAsync();

        if (authenticatedUsers.Users.Length > 0)
        {
          // Store the user ID and name on the config state
          var firstUser = authenticatedUsers.Users[0];
          _configState._twitchWebApiConfig.ChannelID = firstUser.Id;
          _configState._twitchWebApiConfig.ChannelName = firstUser.Login;

          // Save out config on update
          _configState._areConfigSettingsDirty = true;

          bSuccess = true;
        }
      }
      catch (Exception ex)
      {
        EmitLog("Error fetching Twitch API tokens: " + ex.Message);
      }

      return bSuccess;
    }

    bool ConnectToTwichAPI()
    {
      bool bSuccess = false;

      if (_configState._twitchWebApiConfig.AccessToken != "")
      {
        // Make sure the access token is set on the Twitch API settings
        _twitchAPI.Settings.AccessToken = _configState._twitchWebApiConfig.AccessToken;

        // Connect to the Twitch Client API
        _twitchClient = new TwitchClient();
        _twitchClient.Initialize(
          new ConnectionCredentials(
            _configState._twitchWebApiConfig.ChannelID,
            _configState._twitchWebApiConfig.AccessToken),
          _configState._twitchWebApiConfig.ChannelName);
        _twitchClient.OnConnected += OnTwitchClientConnected;
        _twitchClient.OnDisconnected += OnTwitchClientDisconnected;
        _twitchClient.OnIncorrectLogin += OnTwitchIncorrectLogin;
        _twitchClient.OnConnectionError += OnTwitchConnectionError;
        _twitchClient.OnLog += OnTwitchClientLog;
        _twitchClient.OnMessageReceived += OnTwitchClientMessageReceived;

        if (_twitchClient.Connect())
        {
          // Next connect to the Twitch PubSub API (allowed to fail)
          _twitchPubsub = new TwitchPubSub();
          _twitchPubsub.OnPubSubServiceConnected += OnTwitchPubsubConnected;
          _twitchPubsub.OnPubSubServiceClosed += OnTwitchPubsubClosed;
          _twitchPubsub.OnPubSubServiceError += OnTwitchPubsubError;
          _twitchPubsub.OnLog += OnTwitchPubSubLog;
          _twitchPubsub.ListenToChannelPoints(_configState._twitchWebApiConfig.ChannelID);
          _twitchPubsub.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;

          _twitchPubsub.Connect();

          bSuccess = true;
        }
      }

      return bSuccess;
    }

    private void OnTwitchPubSubLog(object sender, TwitchLib.PubSub.Events.OnLogArgs e)
    {
      EmitLog("TwitchPubSub Log: " + e.Data);
    }

    private void OnTwitchClientLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
    {
      EmitLog("TwitchClient Log: " + e.Data);
    }

    void InitializeTwitchOAuthWebServer()
    {
      // Create a new HTTP server locally so that we can make the request for OAUTH token related stuff
      _twitchOAuthWebServer = new HttpServer();
      _twitchOAuthWebServer.EndPoint = new IPEndPoint(IPAddress.Loopback, 8080);
      _twitchOAuthWebServer.RequestReceived += HandleTwitchOAuthRequest;

      _twitchOAuthWebServer.Start();
      EmitLog($"Web server started on: {_twitchOAuthWebServer.EndPoint}");
    }

    async void HandleTwitchOAuthRequest(object sender, HttpRequestEventArgs e)
    {
      using (var writer = new StreamWriter(e.Response.OutputStream))
      {
        if (e.Request.QueryString.AllKeys.Contains("code"))
        {
          var code = e.Request.QueryString["code"];

          // Fetch the Access Token and Refresh Token from Twitch using ClientID and ClientSecret
          if (await FetchAndCacheTwitchAPIConfig(code))
          {
            // Connect to the Twitch Client and PubSub APIs
            ConnectToTwichAPI();
          }
        }
      }
    }
  }
}
