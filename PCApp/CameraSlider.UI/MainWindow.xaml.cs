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

  /*
    public class StreamlabsWebAPISection : ConfigurationSection
    {
        public StreamlabsWebAPISection()
        {
            StreamlabsSocketToken = "";
            TwitchFollowPattern = "NONE";
            TwitchMysterySubPattern = "NONE";
            TwitchSubPattern = "NONE";
            TwitchCheerPattern = "NONE";
            TwitchHostPattern = "NONE";
            TwitchRaidPattern = "NONE";

            TwitchFollowPatternCycles= 5;
            TwitchMysterySubPatternCycles= 5;
            TwitchSubPatternCycles= 5;
            TwitchCheerPatternCycles= 5;
            TwitchHostPatternCycles= 5;
            TwitchRaidPatternCycles= 5;

            TwitchFollowSmokeTime= 0.0f;
            TwitchMysterySubSmokeTime= 0.0f;
            TwitchSubSmokeTime= 0.0f;
            TwitchCheerSmokeTime= 0.0f;
            TwitchHostSmokeTime= 0.0f;
            TwitchRaidSmokeTime= 0.0f;
        }

        [ConfigurationProperty("streamlabs_socket_token")]
        public string StreamlabsSocketToken
        {
            get { return (string)this["streamlabs_socket_token"]; }
            set { this["streamlabs_socket_token"] = value; }
        }

        [ConfigurationProperty("twitch_follow_pattern")]
        public string TwitchFollowPattern
        {
            get { return (string)this["twitch_follow_pattern"]; }
            set { this["twitch_follow_pattern"] = value; }
        }

        [ConfigurationProperty("twitch_mystery_sub_pattern")]
        public string TwitchMysterySubPattern
        {
            get { return (string)this["twitch_mystery_sub_pattern"]; }
            set { this["twitch_mystery_sub_pattern"] = value; }
        }

        [ConfigurationProperty("twitch_sub_pattern")]
        public string TwitchSubPattern
        {
            get { return (string)this["twitch_sub_pattern"]; }
            set { this["twitch_sub_pattern"] = value; }
        }

        [ConfigurationProperty("twitch_cheer_pattern")]
        public string TwitchCheerPattern
        {
            get { return (string)this["twitch_cheer_pattern"]; }
            set { this["twitch_cheer_pattern"] = value; }
        }

        [ConfigurationProperty("twitch_host_pattern")]
        public string TwitchHostPattern
        {
            get { return (string)this["twitch_host_pattern"]; }
            set { this["twitch_host_pattern"] = value; }
        }

        [ConfigurationProperty("twitch_raid_pattern")]
        public string TwitchRaidPattern
        {
            get { return (string)this["twitch_raid_pattern"]; }
            set { this["twitch_raid_pattern"] = value; }
        }

        [ConfigurationProperty("twitch_follow_pattern_cycles")]
        public int TwitchFollowPatternCycles
        {
            get { return (int)this["twitch_follow_pattern_cycles"]; }
            set { this["twitch_follow_pattern_cycles"] = value; }
        }

        [ConfigurationProperty("twitch_mystery_sub_pattern_cycles")]
        public int TwitchMysterySubPatternCycles
        {
            get { return (int)this["twitch_mystery_sub_pattern_cycles"]; }
            set { this["twitch_mystery_sub_pattern_cycles"] = value; }
        }

        [ConfigurationProperty("twitch_sub_pattern_cycles")]
        public int TwitchSubPatternCycles
        {
            get { return (int)this["twitch_sub_pattern_cycles"]; }
            set { this["twitch_sub_pattern_cycles"] = value; }
        }

        [ConfigurationProperty("twitch_cheer_pattern_cycles")]
        public int TwitchCheerPatternCycles
        {
            get { return (int)this["twitch_cheer_pattern_cycles"]; }
            set { this["twitch_cheer_pattern_cycles"] = value; }
        }

        [ConfigurationProperty("twitch_host_pattern_cycles")]
        public int TwitchHostPatternCycles
        {
            get { return (int)this["twitch_host_pattern_cycles"]; }
            set { this["twitch_host_pattern_cycles"] = value; }
        }

        [ConfigurationProperty("twitch_raid_pattern_cycles")]
        public int TwitchRaidPatternCycles
        {
            get { return (int)this["twitch_raid_pattern_cycles"]; }
            set { this["twitch_raid_pattern_cycles"] = value; }
        }

        [ConfigurationProperty("twitch_follow_smoke_time")]
        public float TwitchFollowSmokeTime
        {
            get { return (float)this["twitch_follow_smoke_time"]; }
            set { this["twitch_follow_smoke_time"] = value; }
        }

        [ConfigurationProperty("twitch_mystery_sub_smoke_time")]
        public float TwitchMysterySubSmokeTime
        {
            get { return (float)this["twitch_mystery_sub_smoke_time"]; }
            set { this["twitch_mystery_sub_smoke_time"] = value; }
        }

        [ConfigurationProperty("twitch_sub_smoke_time")]
        public float TwitchSubSmokeTime
        {
            get { return (float)this["twitch_sub_smoke_time"]; }
            set { this["twitch_sub_smoke_time"] = value; }
        }

        [ConfigurationProperty("twitch_cheer_smoke_time")]
        public float TwitchCheerSmokeTime
        {
            get { return (float)this["twitch_cheer_smoke_time"]; }
            set { this["twitch_cheer_smoke_time"] = value; }
        }

        [ConfigurationProperty("twitch_host_smoke_time")]
        public float TwitchHostSmokeTime
        {
            get { return (float)this["twitch_host_smoke_time"]; }
            set { this["twitch_host_smoke_time"] = value; }
        }

        [ConfigurationProperty("twitch_raid_smoke_time")]
        public float TwitchRaidSmokeTime
        {
            get { return (float)this["twitch_raid_smoke_time"]; }
            set { this["twitch_raid_smoke_time"] = value; }
        }
    }
  */
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Configuration _configFile;
        private TwitchWebAPISection _twitchWebApiConfig;
        private ObsConfigSection _obsConfig;

        private string _selectedDeviceId = "";
        private CameraSlider.Bluetooth.CameraSliderDeviceWatcher _pairedWatcher;
        private bool _deviceReconnectionPending = false;
        private CameraSliderDevice _cameraSliderDevice;

        private bool _twitchIsConnected = false;
        private bool _twitchReconnectionPending = false;
        private ITwitchAPI _twitchAPI;
        private TwitchPubSub _twitchPubsubClient;
        
        private bool _obsReconnectionPending = false;
        private OBSWebsocket _obsClient;

        private Timer _deviceWatchdogTimer = null;
        private static int _keepAliveDelay = 10;
        private int _keepAliveCountdown = _keepAliveDelay;

        private Timer _twitchWatchdogTimer = null;

        private Timer _obsWatchdogTimer = null;

        public MainWindow()
        {
            InitializeComponent();

            _selectedDeviceId = "";
            _pairedWatcher = new CameraSlider.Bluetooth.CameraSliderDeviceWatcher(DeviceSelector.BluetoothLePairedOnly);
            _pairedWatcher.DeviceAdded += OnPaired_DeviceAdded;
            _pairedWatcher.DeviceRemoved += OnPaired_DeviceRemoved;
            _pairedWatcher.Start();

            // Registern Twitch API and PubSub to get Twitch specific events (channel point redeems)
            _twitchAPI = new TwitchAPI();
            _twitchPubsubClient = new TwitchPubSub();
            _twitchPubsubClient.OnPubSubServiceConnected += OnTwitchPubsubConnected;
            _twitchPubsubClient.OnPubSubServiceClosed += OnTwitchPubsubClosed;
            _twitchPubsubClient.OnPubSubServiceError += OnTwitchPubsubError;

            _obsClient = new OBSWebsocket();
            _obsClient.Connected += OnObsConnected;
            _obsClient.OBSExit += OnObsExited;
            _obsClient.Disconnected += OnObsDisconnected;

            // Register to Bluetooth LE Camera Slider Device Manager
            _cameraSliderDevice = new CameraSliderDevice();
            _cameraSliderDevice.ConnectionStatusChanged += DLDeviceOnDeviceConnectionStatusChanged;
            _cameraSliderDevice.CameraSliderEventHandler += CameraSliderEventReceived;

            // Load the config file and update the UI based on safed settings
            LoadConfig();
            ApplyConfigToUI();

            // Timer use to maintain connection to device
            _deviceWatchdogTimer = new Timer(DeviceWatchdogCallback, null, 0, 1000);            
            _twitchWatchdogTimer = new Timer(TwitchWatchdogCallback, null, 0, 1000);
            _obsWatchdogTimer = new Timer(ObsStudioWatchdogCallback, null, 0, 1000);
        }

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

            _configFile.Save();
        }

        protected void ApplyConfigToUI()
        {
            //string[] Patterns = new string[] { }; //Enum.GetNames(typeof(CameraSliderDevice.Pattern));
            //TwitchCheerPattern.ItemsSource = Patterns;
            //TwitchFollowPattern.ItemsSource = Patterns;
            //TwitchHostPattern.ItemsSource = Patterns;
            //TwitchMysterySubPattern.ItemsSource = Patterns;
            //TwitchSubPattern.ItemsSource = Patterns;
            //TwitchRaidPattern.ItemsSource = Patterns;

            TwitchClientIdInput.Text = _twitchWebApiConfig.ClientID;
            TwitchSecretKeyInput.Password = _twitchWebApiConfig.Secret;
            //SetPatternComboBox(TwitchCheerPattern, _streamlabsWebApiConfig.TwitchCheerPattern);
            //SetPatternComboBox(TwitchFollowPattern, _streamlabsWebApiConfig.TwitchFollowPattern);
            //SetPatternComboBox(TwitchHostPattern, _streamlabsWebApiConfig.TwitchHostPattern);
            //SetPatternComboBox(TwitchMysterySubPattern, _streamlabsWebApiConfig.TwitchMysterySubPattern);
            //SetPatternComboBox(TwitchSubPattern, _streamlabsWebApiConfig.TwitchSubPattern);
            //SetPatternComboBox(TwitchRaidPattern, _streamlabsWebApiConfig.TwitchRaidPattern);
            //TwitchCheerCyclesInput.Text= _streamlabsWebApiConfig.TwitchCheerPatternCycles.ToString();
            //TwitchFollowCyclesInput.Text= _streamlabsWebApiConfig.TwitchFollowPatternCycles.ToString();
            //TwitchHostCyclesInput.Text= _streamlabsWebApiConfig.TwitchHostPatternCycles.ToString();
            //TwitchMysterySubCyclesInput.Text= _streamlabsWebApiConfig.TwitchMysterySubPatternCycles.ToString();
            //TwitchSubCyclesInput.Text= _streamlabsWebApiConfig.TwitchSubPatternCycles.ToString();
            //TwitchRaidCyclesInput.Text= _streamlabsWebApiConfig.TwitchRaidPatternCycles.ToString();
            //TwitchCheerSmokeInput.Text = _streamlabsWebApiConfig.TwitchCheerSmokeTime.ToString();
            //TwitchFollowSmokeInput.Text = _streamlabsWebApiConfig.TwitchFollowSmokeTime.ToString();
            //TwitchHostSmokeInput.Text = _streamlabsWebApiConfig.TwitchHostSmokeTime.ToString();
            //TwitchMysterySubSmokeInput.Text = _streamlabsWebApiConfig.TwitchMysterySubSmokeTime.ToString();
            //TwitchSubSmokeInput.Text = _streamlabsWebApiConfig.TwitchSubSmokeTime.ToString();
            //TwitchRaidSmokeInput.Text = _streamlabsWebApiConfig.TwitchRaidSmokeTime.ToString();

            SocketAddressInput.Text = _obsConfig.SocketAddress;
            SocketPasswordInput.Password = _obsConfig.Password;
        }

        protected void SetPatternComboBox(ComboBox comboBox, string pattern)
        {
            int newIndex= comboBox.Items.IndexOf(pattern);

            if (newIndex != -1)
            {
                comboBox.SelectedIndex = newIndex;
            }
        }

        protected void SaveConfig()
        {
            _configFile.Save();
        }

        protected async override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            _pairedWatcher.Stop();

            if (_cameraSliderDevice.IsConnected)
            {
                await _cameraSliderDevice.DisconnectAsync();
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
                    Debug.WriteLine("Device connect error: " + ex.Message);
                }
                _deviceReconnectionPending = false;
            }
        }

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

        private async void CameraSliderEventReceived(object sender, CameraSliderEventArgs arg)
        {
            await RunOnUiThread(() =>
            {
                d("Got new message: " + arg.Message);
            });
        }

        private async void DLDeviceOnDeviceConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            d("Current connection status is: " + args.IsConnected);

            await RunOnUiThread(async () =>
            {
                bool connected = args.IsConnected;
                if (connected)
                {
                    DeviceTxtStatus.Content = "Connected";
                }
                else
                {
                    DeviceTxtStatus.Content = "Searching...";
                }

                BtnGotoCamera.IsEnabled = connected;
            });
        }

    private async void TwitchWatchdogCallback(Object context)
    {
      // Attempt reconnection to stream labs
      if (_twitchWebApiConfig.ChannelID != "" && 
          _twitchWebApiConfig.ClientID != "" &&
          _twitchWebApiConfig.Secret != "" &&
          !_twitchReconnectionPending && 
          !_twitchIsConnected)
      {
        await RunOnUiThread(() =>
        {
          TwitchTxtStatus.Content = "Connecting...";
        });


        _twitchReconnectionPending = true;
        _twitchAPI.Settings.ClientId = _twitchWebApiConfig.ClientID;
        _twitchAPI.Settings.Secret = _twitchWebApiConfig.Secret;
        _twitchPubsubClient.ListenToChannelPoints(_twitchWebApiConfig.ChannelID);
        _twitchPubsubClient.OnChannelPointsRewardRedeemed += OnChannelPointsRewardRedeemed;
        _twitchPubsubClient.Connect();
      }
    }

    private void OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
    {
      var redemption = e.RewardRedeemed.Redemption;
      var reward = e.RewardRedeemed.Redemption.Reward;
      var redeemedUser = e.RewardRedeemed.Redemption.User;
      string rewardId= reward.Id;

      //if (redemption.Status == "UNFULFILLED")
      //{

      //  d($"{redeemedUser.DisplayName} redeemed: {reward.Title}");
      //  await TwitchLib.API.Helix.ChannelPoints.UpdateRedemptionStatusAsync(e.ChannelId, reward.Id,
      //      new List<string>() { e.RewardRedeemed.Redemption.Id }, new UpdateCustomRewardRedemptionStatusRequest() { Status = CustomRewardRedemptionStatus.CANCELED });
      //}

      //if (redemption.Status == "FULFILLED")
      //{
      //  d($"Reward from {redeemedUser.DisplayName} ({reward.Title}) has been marked as complete");
      //}
      //TODO Trigger a camera scene based on the redeem
    }

    private async void OnTwitchPubsubError(object sender, OnPubSubServiceErrorArgs e)
    {
      if (_twitchReconnectionPending)
      {
        await RunOnUiThread(() =>
        {
          d("Twitch PubSub failed to connect");
          TwitchTxtStatus.Content = "Disconnected";
        });

        _twitchReconnectionPending = false;
      }
    }

    private async void OnTwitchPubsubClosed(object sender, EventArgs e)
    {
      await RunOnUiThread(() =>
      {
        d("Twitch PubSub disconnected");
        TwitchTxtStatus.Content = "Disconnected";
      });

      _twitchIsConnected = true;
      _twitchReconnectionPending = false;
    }

    private async void OnTwitchPubsubConnected(object sender, EventArgs e)
    {
      await RunOnUiThread(() =>
      {
        d("Twitch PubSub connected");
        TwitchTxtStatus.Content = "Connected";
      });

      _twitchIsConnected = true;
    }

    //protected async void OnStreamlabsEvent(object sender, StreamlabsEventArgs e)
    //{
    //    if (e.Data is StreamlabsEvent<TwitchFollow>)
    //    {
    //        //await PlayPattern(
    //        //    _streamlabsWebApiConfig.TwitchFollowPattern, 
    //        //    _streamlabsWebApiConfig.TwitchFollowPatternCycles);
    //    }
    //    else if (e.Data is StreamlabsEvent<TwitchMysterySubscription>)
    //    {
    //        //await PlayPattern(
    //        //    _streamlabsWebApiConfig.TwitchMysterySubPattern, 
    //        //    _streamlabsWebApiConfig.TwitchMysterySubPatternCycles);
    //    }
    //    else if (e.Data is StreamlabsEvent<TwitchSubscription>)
    //    {
    //        //await PlayPattern(
    //        //    _streamlabsWebApiConfig.TwitchSubPattern,
    //        //    _streamlabsWebApiConfig.TwitchSubPatternCycles);
    //    }
    //    else if (e.Data is StreamlabsEvent<TwitchCheer>)
    //    {
    //        //await PlayPattern(
    //        //    _streamlabsWebApiConfig.TwitchCheerPattern,
    //        //    _streamlabsWebApiConfig.TwitchCheerPatternCycles);
    //    }
    //    else if (e.Data is StreamlabsEvent<TwitchHost>)
    //    {
    //        //await PlayPattern(
    //        //    _streamlabsWebApiConfig.TwitchHostPattern,
    //        //    _streamlabsWebApiConfig.TwitchHostPatternCycles);
    //    }
    //    else if (e.Data is StreamlabsEvent<TwitchRaid>)
    //    {
    //        //await PlayPattern(
    //        //    _streamlabsWebApiConfig.TwitchRaidPattern,
    //        //    _streamlabsWebApiConfig.TwitchRaidPatternCycles);
    //    }
    //}


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

        //private async void BtnStart_Click(object sender, RoutedEventArgs e)
        //{
        //    d("Button START clicked.");
        //    //await _domeLightsMonitor.EnableNotificationsAsync();
        //    d("Notification enabled");
        //}

        //private async void BtnStop_Click(object sender, RoutedEventArgs e)
        //{
        //    d("Button STOP clicked.");
        //    //await _domeLightsMonitor.DisableNotificationsAsync();
        //    d("Notification disabled.");
        //}

        //private async void BtnReadInfo_Click(object sender, RoutedEventArgs e)
        //{
        //    var deviceInfo = await _cameraSliderDevice.GetDeviceInfoAsync();

        //    d($" Manufacturer : {deviceInfo.Manufacturer}"); d("");
        //    d($"    Model : {deviceInfo.ModelNumber}"); d("");
        //    d($"      S/N : {deviceInfo.SerialNumber}"); d("");
        //    d($" Firmware : {deviceInfo.Firmware}"); d("");
        //    d($" Hardware : {deviceInfo.Hardware}"); d("");
        //}

        [Conditional("DEBUG")]
        private void d(string txt)
        {
            Debug.WriteLine(txt);
        }

        private async Task RunOnUiThread(Action a)
        {
           await this.Dispatcher.InvokeAsync(() =>
           {
               a();
           });
        }
    //private async void RainbowCycle_Click(object sender, RoutedEventArgs e)
    //{
    //    //await PlayPattern("RAINBOW_CYCLE", 5);
    //}

    //private async void TheaterChase_Click(object sender, RoutedEventArgs e)
    //{
    //    //await PlayPattern("THEATER_CHASE", 5);
    //}

    //private async void Scanner_Click(object sender, RoutedEventArgs e)
    //{
    //    //await PlayPattern("SCANNER", 5);
    //}

    //private async void Fade_Click(object sender, RoutedEventArgs e)
    //{
    //    //await PlayPattern("FADE", 5);
    //}

    //private async void BtnColorWipe_Click(object sender, RoutedEventArgs e)
    //{
    //    //await PlayPattern("COLOR_WIPE", 5);
    //}

    //private async Task PlayPattern(string patternString, int CycleCount)
    //{
    //    if (!_cameraSliderDevice.IsConnected)
    //        return;

    //    CameraSliderDevice.Pattern pattern= CameraSliderDevice.Pattern.NONE;
    //    if (!Enum.TryParse<CameraSliderDevice.Pattern>(patternString, out pattern))
    //        return;

    //    if (_obsClient.IsConnected)
    //    {
    //        SetObsSceneForDuration(_obsConfig.PatternSceneName, _obsConfig.PatternSceneDuration).ConfigureAwait(false);
    //    }
    //    else
    //    {
    //        SetSlobsSceneForDuration(_slobsConfig.PatternSceneName, _slobsConfig.PatternSceneDuration).ConfigureAwait(false);
    //    }

    //    // Give SLOBS a bit to change the scene
    //    await Task.Delay(500);

    //    switch (pattern)
    //    {
    //        case CameraSliderDevice.Pattern.NONE:
    //            await _cameraSliderDevice.CancelPatternAsync();
    //            break;
    //        case CameraSliderDevice.Pattern.RAINBOW_CYCLE:
    //            await _cameraSliderDevice.PlayRainbowCycleAsync(CycleCount);
    //            break;
    //        case CameraSliderDevice.Pattern.THEATER_CHASE:
    //            await _cameraSliderDevice.PlayTheaterChaseAsync(Color.Red, Color.BlueViolet, CycleCount);
    //            break;
    //        case CameraSliderDevice.Pattern.COLOR_WIPE:
    //            await _cameraSliderDevice.PlayColorWipeAsync(Color.Red, CycleCount);
    //            break;
    //        case CameraSliderDevice.Pattern.SCANNER:
    //            await _cameraSliderDevice.PlayScannerAsync(Color.Red, CycleCount);
    //            break;
    //        case CameraSliderDevice.Pattern.FADE:
    //            await _cameraSliderDevice.PlayFadeAsync(Color.Red, Color.BlueViolet, 16, CycleCount);
    //            break;
    //    }
    //}

    private void TwitchChannelIdInput_Changed(object sender, RoutedEventArgs e)
    {
      _twitchWebApiConfig.ChannelID = TwitchClientIdInput.Text;
      SaveConfig();

      _twitchPubsubClient.Disconnect();
    }

    private void TwitchClientIdInput_Changed(object sender, RoutedEventArgs e)
    {
      _twitchWebApiConfig.ClientID = TwitchClientIdInput.Text;
      SaveConfig();

      _twitchPubsubClient.Disconnect();
    }

    private void TwitchSecretInput_Changed(object sender, RoutedEventArgs e)
    {
            _twitchWebApiConfig.Secret = TwitchSecretKeyInput.Password;
            SaveConfig();

            _twitchPubsubClient.Disconnect();
        }

    private void TwitchFollowPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
     // _twitchWebApiConfig.TwitchFollowPattern = TwitchFollowPattern.SelectedItem.ToString();
      //SaveConfig();
    }

    private void TwitchFollowCyclesInput_TextChanged(object sender, TextChangedEventArgs e)
    {
    //    int Cycles = 0;
    //    if (int.TryParse(TwitchFollowCyclesInput.Text, out Cycles))
    //    {
    //        _twitchWebApiConfig.TwitchFollowPatternCycles = Cycles;
    //        SaveConfig();
    //    }
    }

    private void TwitchFollowSmokeInput_TextChanged(object sender, TextChangedEventArgs e)
    {
    //    float SmokeTime = 0;
    //    if (float.TryParse(TwitchFollowSmokeInput.Text, out SmokeTime))
    //    {
    //        _twitchWebApiConfig.TwitchFollowSmokeTime = SmokeTime;
    //        SaveConfig();
    //    }
    }

    //private void SlobsLedSceneNameInput_TextChanged(object sender, TextChangedEventArgs e)
    //{
    //    _slobsConfig.PatternSceneName = SlobsLedSceneNameInput.Text;
    //    SaveConfig();
    //}

    //private void SlobsLedSceneDurationInput_TextChanged(object sender, TextChangedEventArgs e)
    //{
    //    int Duration = 0;
    //    if (int.TryParse(SlobsLedSceneDurationInput.Text, out Duration))
    //    {
    //        _slobsConfig.PatternSceneDuration = Duration;
    //        SaveConfig();
    //    }
    //}        

    private void NumericalInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        //private void ObsLedSceneNameInput_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    _obsConfig.PatternSceneName = ObsLedSceneNameInput.Text;
        //    SaveConfig();
        //}

        //private void ObsLedSceneDurationInput_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    int Duration = 0;
        //    if (int.TryParse(ObsLedSceneDurationInput.Text, out Duration))
        //    {
        //        _obsConfig.PatternSceneDuration = Duration;
        //        SaveConfig();
        //    }
        //}

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

    private void BtnGotoCamera_Click(object sender, RoutedEventArgs e)
    {

    }
  }
}
