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
using System.Windows.Navigation;
using CameraSlider.UI;
using System.Diagnostics;
using CameraSlider.Bluetooth.Events;
using System.ComponentModel;
using Color = System.Drawing.Color;
using StreamlabsEventReceiver;
using SLOBSharp.Client;
using SLOBSharp.Client.Requests;
using CameraSlider.Bluetooth;
using CameraSlider.Bluetooth.Schema;
using System.Collections.ObjectModel;
using System.Threading;
using System.Configuration;
using System.Text.RegularExpressions;
using OBSWebsocketDotNet;

namespace CameraSlider.UI
{
    public class SlobsConfigSection : ConfigurationSection
    {
        public SlobsConfigSection()
        {
            PatternSceneName = "Main";
            PatternSceneDuration = 3000;
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
    }

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

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string SLOBS_ERROR_RESULT= "<ERROR>";

        private Configuration _configFile;
        private SlobsConfigSection _slobsConfig;
        private StreamlabsWebAPISection _streamlabsWebApiConfig;
        private ObsConfigSection _obsConfig;

        private string _selectedDeviceId = "";
        private CameraSlider.Bluetooth.DomeLightsDeviceWatcher _pairedWatcher;
        private bool _deviceReconnectionPending = false;
        private DomeLightsDevice _domeLightsDevice;

        private bool _streamlabsReconnectionPending = false;
        private StreamlabsEventClient _streamlabsEventClient;
        
        private SlobsClient _slobsClient;

        private bool _obsReconnectionPending = false;
        private OBSWebsocket _obsClient;

        private Timer _deviceWatchdogTimer = null;
        private static int _keepAliveDelay = 10;
        private int _keepAliveCountdown = _keepAliveDelay;

        private Timer _streamlabsWatchdogTimer = null;

        private Timer _obsWatchdogTimer = null;

        public MainWindow()
        {
            InitializeComponent();

            _selectedDeviceId = "";
            _pairedWatcher = new CameraSlider.Bluetooth.DomeLightsDeviceWatcher(DeviceSelector.BluetoothLePairedOnly);
            _pairedWatcher.DeviceAdded += OnPaired_DeviceAdded;
            _pairedWatcher.DeviceRemoved += OnPaired_DeviceRemoved;
            _pairedWatcher.Start();

            // Register to Streamlabs OBS named pipe so that we can remote control SLOBS
            // Constructor takes the name of the pipe (default constructor uses the pipe name "slobs")
            _slobsClient = new SlobsPipeClient("slobs");

            // Register to Streamlabs WebSocket API to get multiplatform stream events (subs, bits, etc)
            _streamlabsEventClient = new StreamlabsEventClient();
            _streamlabsEventClient.StreamlabsSocketConnected += OnStreamlabsConnected;
            _streamlabsEventClient.StreamlabsSocketConnectFailed += OnStreamlabsConnectFailed;
            _streamlabsEventClient.StreamlabsSocketDisconnected += OnStreamlabsDisconnected;
            _streamlabsEventClient.StreamlabsSocketEvent += OnStreamlabsEvent;

            _obsClient = new OBSWebsocket();
            _obsClient.Connected += OnObsConnected;
            _obsClient.OBSExit += OnObsExited;
            _obsClient.Disconnected += OnObsDisconnected;

            // Register to Bluetooth LE Domelights Device Manager
            _domeLightsDevice = new DomeLightsDevice();
            _domeLightsDevice.ConnectionStatusChanged += DLDeviceOnDeviceConnectionStatusChanged;
            _domeLightsDevice.UARTMessageHandler += UARTMessageReceived;

            // Load the config file and update the UI based on safed settings
            LoadConfig();
            ApplyConfigToUI();

            // Timer use to maintain connection to device
            _deviceWatchdogTimer = new Timer(DeviceWatchdogCallback, null, 0, 1000);
            _obsWatchdogTimer = new Timer(ObsStudioWatchdogCallback, null, 0, 1000);
        }

        protected void LoadConfig()
        {
            // Open App.Config of executable
            _configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            _slobsConfig = (SlobsConfigSection)_configFile.GetSection("slobs_settings");
            if (_slobsConfig == null)
            {
                _slobsConfig = new SlobsConfigSection();
                _configFile.Sections.Add("slobs_settings", _slobsConfig);
            }

            _streamlabsWebApiConfig = (StreamlabsWebAPISection)_configFile.GetSection("streamlabs_webapi_settings");
            if (_streamlabsWebApiConfig == null)
            {
                _streamlabsWebApiConfig = new StreamlabsWebAPISection();
                _configFile.Sections.Add("streamlabs_webapi_settings", _streamlabsWebApiConfig);
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
            string[] Patterns = Enum.GetNames(typeof(DomeLightsDevice.Pattern));
            TwitchCheerPattern.ItemsSource = Patterns;
            TwitchFollowPattern.ItemsSource = Patterns;
            TwitchHostPattern.ItemsSource = Patterns;
            TwitchMysterySubPattern.ItemsSource = Patterns;
            TwitchSubPattern.ItemsSource = Patterns;
            TwitchRaidPattern.ItemsSource = Patterns;

            StreamlabsSocketKeyInput.Password = _streamlabsWebApiConfig.StreamlabsSocketToken;
            SetPatternComboBox(TwitchCheerPattern, _streamlabsWebApiConfig.TwitchCheerPattern);
            SetPatternComboBox(TwitchFollowPattern, _streamlabsWebApiConfig.TwitchFollowPattern);
            SetPatternComboBox(TwitchHostPattern, _streamlabsWebApiConfig.TwitchHostPattern);
            SetPatternComboBox(TwitchMysterySubPattern, _streamlabsWebApiConfig.TwitchMysterySubPattern);
            SetPatternComboBox(TwitchSubPattern, _streamlabsWebApiConfig.TwitchSubPattern);
            SetPatternComboBox(TwitchRaidPattern, _streamlabsWebApiConfig.TwitchRaidPattern);
            TwitchCheerCyclesInput.Text= _streamlabsWebApiConfig.TwitchCheerPatternCycles.ToString();
            TwitchFollowCyclesInput.Text= _streamlabsWebApiConfig.TwitchFollowPatternCycles.ToString();
            TwitchHostCyclesInput.Text= _streamlabsWebApiConfig.TwitchHostPatternCycles.ToString();
            TwitchMysterySubCyclesInput.Text= _streamlabsWebApiConfig.TwitchMysterySubPatternCycles.ToString();
            TwitchSubCyclesInput.Text= _streamlabsWebApiConfig.TwitchSubPatternCycles.ToString();
            TwitchRaidCyclesInput.Text= _streamlabsWebApiConfig.TwitchRaidPatternCycles.ToString();
            TwitchCheerSmokeInput.Text = _streamlabsWebApiConfig.TwitchCheerSmokeTime.ToString();
            TwitchFollowSmokeInput.Text = _streamlabsWebApiConfig.TwitchFollowSmokeTime.ToString();
            TwitchHostSmokeInput.Text = _streamlabsWebApiConfig.TwitchHostSmokeTime.ToString();
            TwitchMysterySubSmokeInput.Text = _streamlabsWebApiConfig.TwitchMysterySubSmokeTime.ToString();
            TwitchSubSmokeInput.Text = _streamlabsWebApiConfig.TwitchSubSmokeTime.ToString();
            TwitchRaidSmokeInput.Text = _streamlabsWebApiConfig.TwitchRaidSmokeTime.ToString();

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

            if (_domeLightsDevice.IsConnected)
            {
                await _domeLightsDevice.DisconnectAsync();
            }
        }

        private void OnPaired_DeviceAdded(object sender, CameraSlider.Bluetooth.Events.DeviceAddedEventArgs e)
        {
            Debug.WriteLine("Paired Device Added: " + e.Device.Id);

            if (e.Device.Name == "DomeLights" && _selectedDeviceId == "")
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
            if (_domeLightsDevice.IsConnected)
            {
                _keepAliveCountdown--;
                if (_keepAliveCountdown <= 0)
                {
                    await _domeLightsDevice.WakeUp();
                    _keepAliveCountdown = _keepAliveDelay;
                }
            }
            // Attempt to kick off an async device reconnection
            else if (_selectedDeviceId != "" && !_deviceReconnectionPending && !_domeLightsDevice.IsConnected)
            {
                _deviceReconnectionPending = true;
                try
                {
                    await _domeLightsDevice.ConnectAsync(_selectedDeviceId);
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

        private async void UARTMessageReceived(object sender, UARTMessageEventArgs arg)
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
                    var device = await _domeLightsDevice.GetDeviceInfoAsync();
                    DeviceTxtStatus.Content = "Connected";
                }
                else
                {
                    DeviceTxtStatus.Content = "Searching...";
                }

                BtnRainbowCycle.IsEnabled = connected;
            });
        }

        protected async Task<Dictionary<string, string>> GetSlobsSceneNameToIdTable()
        {
            try
            {
                var nameToIdTable = new Dictionary<string, string>();
                var slobsRequest = SlobsRequestBuilder.NewRequest().SetMethod("getScenes").SetResource("ScenesService").BuildRequest();
                var slobsRpcResponse = await _slobsClient.ExecuteRequestAsync(slobsRequest).ConfigureAwait(false);

                foreach (var result in slobsRpcResponse.Result)
                {
                    nameToIdTable.Add(result.Name, result.Id);
                }

                return nameToIdTable;
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }
        }

        protected async Task<string> GetSlobsSceneName()
        {
            try
            {
                var slobsRequest = SlobsRequestBuilder.NewRequest().SetMethod("activeScene").SetResource("ScenesService").BuildRequest();
                var slobsRpcResponse = await _slobsClient.ExecuteRequestAsync(slobsRequest).ConfigureAwait(false);

                return slobsRpcResponse.Result.FirstOrDefault().Name;
            }
            catch (System.InvalidOperationException)
            {
                return SLOBS_ERROR_RESULT;
            }
        }

        protected async Task<bool> SetSlobsSceneByName(string SceneName)
        {
            Dictionary<string, string> nameToIdTable= await GetSlobsSceneNameToIdTable();
            if (nameToIdTable == null)
            {
                return false;
            }

            string SceneId;
            if (!nameToIdTable.TryGetValue(SceneName, out SceneId))
            {
                return false;
            }

            try
            {
                var slobsRequest = SlobsRequestBuilder.NewRequest().AddArgs(SceneId).SetMethod("makeSceneActive").SetResource("ScenesService").BuildRequest();
                var slobsRpcResponse = await _slobsClient.ExecuteRequestAsync(slobsRequest).ConfigureAwait(false);

                return true;
            }
            catch (System.InvalidOperationException)
            {
                return false;
            }
        }

        protected async Task SetSlobsSceneForDuration(string NewSceneName, int Milliseconds)
        {
            if (NewSceneName != "")
            {
                string OrigSceneName = await GetSlobsSceneName();
                if (OrigSceneName != NewSceneName)
                {
                    bool bSuccess= await SetSlobsSceneByName(NewSceneName);
                    if (bSuccess)
                    {
                        await Task.Delay(Milliseconds);
                        await SetSlobsSceneByName(OrigSceneName);
                    }
                }
            }
        }

        protected async void OnStreamlabsConnected(object sender, EventArgs e)
        {
            await RunOnUiThread(() =>
            {
                d("Streamlabs connected");
                StreamlabsTxtStatus.Content = "Connected";
            });
        }
        
        protected async void OnStreamlabsConnectFailed(object sender, EventArgs e)
        {
            await RunOnUiThread(() =>
            {
                d("Streamlabs failed to connect");
                StreamlabsTxtStatus.Content = "Disconnected";
            });

            _streamlabsReconnectionPending = false;
        }

        protected async void OnStreamlabsDisconnected(object sender, EventArgs e)
        {
            await RunOnUiThread(() =>
            {
                d("Streamlabs disconnected");
                StreamlabsTxtStatus.Content = "Disconnected";
            });

            _streamlabsReconnectionPending = false;
        }

        protected async void OnStreamlabsEvent(object sender, StreamlabsEventArgs e)
        {
            if (e.Data is StreamlabsEvent<TwitchFollow>)
            {
                await EmitSmoke(_streamlabsWebApiConfig.TwitchFollowSmokeTime);
                await PlayPattern(
                    _streamlabsWebApiConfig.TwitchFollowPattern, 
                    _streamlabsWebApiConfig.TwitchFollowPatternCycles);
            }
            else if (e.Data is StreamlabsEvent<TwitchMysterySubscription>)
            {
                await EmitSmoke(_streamlabsWebApiConfig.TwitchMysterySubSmokeTime);
                await PlayPattern(
                    _streamlabsWebApiConfig.TwitchMysterySubPattern, 
                    _streamlabsWebApiConfig.TwitchMysterySubPatternCycles);
            }
            else if (e.Data is StreamlabsEvent<TwitchSubscription>)
            {
                await EmitSmoke(_streamlabsWebApiConfig.TwitchSubSmokeTime);
                await PlayPattern(
                    _streamlabsWebApiConfig.TwitchSubPattern,
                    _streamlabsWebApiConfig.TwitchSubPatternCycles);
            }
            else if (e.Data is StreamlabsEvent<TwitchCheer>)
            {
                await EmitSmoke(_streamlabsWebApiConfig.TwitchCheerSmokeTime);
                await PlayPattern(
                    _streamlabsWebApiConfig.TwitchCheerPattern,
                    _streamlabsWebApiConfig.TwitchCheerPatternCycles);
            }
            else if (e.Data is StreamlabsEvent<TwitchHost>)
            {
                await EmitSmoke(_streamlabsWebApiConfig.TwitchHostSmokeTime);
                await PlayPattern(
                    _streamlabsWebApiConfig.TwitchHostPattern,
                    _streamlabsWebApiConfig.TwitchHostPatternCycles);
            }
            else if (e.Data is StreamlabsEvent<TwitchRaid>)
            {
                await EmitSmoke(_streamlabsWebApiConfig.TwitchRaidSmokeTime);
                await PlayPattern(
                    _streamlabsWebApiConfig.TwitchRaidPattern,
                    _streamlabsWebApiConfig.TwitchRaidPatternCycles);
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

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            d("Button START clicked.");
            //await _domeLightsMonitor.EnableNotificationsAsync();
            d("Notification enabled");
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            d("Button STOP clicked.");
            //await _domeLightsMonitor.DisableNotificationsAsync();
            d("Notification disabled.");
        }

        private async void BtnReadInfo_Click(object sender, RoutedEventArgs e)
        {
            var deviceInfo = await _domeLightsDevice.GetDeviceInfoAsync();

            d($" Manufacturer : {deviceInfo.Manufacturer}"); d("");
            d($"    Model : {deviceInfo.ModelNumber}"); d("");
            d($"      S/N : {deviceInfo.SerialNumber}"); d("");
            d($" Firmware : {deviceInfo.Firmware}"); d("");
            d($" Hardware : {deviceInfo.Hardware}"); d("");
        }

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
        private async void RainbowCycle_Click(object sender, RoutedEventArgs e)
        {
            await PlayPattern("RAINBOW_CYCLE", 5);
        }

        private async void TheaterChase_Click(object sender, RoutedEventArgs e)
        {
            await PlayPattern("THEATER_CHASE", 5);
        }

        private async void Scanner_Click(object sender, RoutedEventArgs e)
        {
            await PlayPattern("SCANNER", 5);
        }

        private async void Fade_Click(object sender, RoutedEventArgs e)
        {
            await PlayPattern("FADE", 5);
        }

        private async void BtnColorWipe_Click(object sender, RoutedEventArgs e)
        {
            await PlayPattern("COLOR_WIPE", 5);
        }
        private async void EmitSmoke_Click(object sender, RoutedEventArgs e)
        {
            float SmokeTime = 0;
            if (float.TryParse(TestSmokeInput.Text, out SmokeTime))
            {
                await EmitSmoke(SmokeTime);
            }
        }

        private async Task EmitSmoke(float SmokeTime)
        {
            if (SmokeTime > 0.0f)
            {
                short Milliseconds = (short)(SmokeTime * 1000.0f);
                await _domeLightsDevice.EmitSmokeAsync(Milliseconds);
            }
        }

        private async Task PlayPattern(string patternString, int CycleCount)
        {
            if (!_domeLightsDevice.IsConnected)
                return;

            DomeLightsDevice.Pattern pattern= DomeLightsDevice.Pattern.NONE;
            if (!Enum.TryParse<DomeLightsDevice.Pattern>(patternString, out pattern))
                return;

            if (_obsClient.IsConnected)
            {
                SetObsSceneForDuration(_obsConfig.PatternSceneName, _obsConfig.PatternSceneDuration).ConfigureAwait(false);
            }
            else
            {
                SetSlobsSceneForDuration(_slobsConfig.PatternSceneName, _slobsConfig.PatternSceneDuration).ConfigureAwait(false);
            }

            // Give SLOBS a bit to change the scene
            await Task.Delay(500);

            switch (pattern)
            {
                case DomeLightsDevice.Pattern.NONE:
                    await _domeLightsDevice.CancelPatternAsync();
                    break;
                case DomeLightsDevice.Pattern.RAINBOW_CYCLE:
                    await _domeLightsDevice.PlayRainbowCycleAsync(CycleCount);
                    break;
                case DomeLightsDevice.Pattern.THEATER_CHASE:
                    await _domeLightsDevice.PlayTheaterChaseAsync(Color.Red, Color.BlueViolet, CycleCount);
                    break;
                case DomeLightsDevice.Pattern.COLOR_WIPE:
                    await _domeLightsDevice.PlayColorWipeAsync(Color.Red, CycleCount);
                    break;
                case DomeLightsDevice.Pattern.SCANNER:
                    await _domeLightsDevice.PlayScannerAsync(Color.Red, CycleCount);
                    break;
                case DomeLightsDevice.Pattern.FADE:
                    await _domeLightsDevice.PlayFadeAsync(Color.Red, Color.BlueViolet, 16, CycleCount);
                    break;
            }
        }

        private void StreamlabsSocketKeyInput_PasswordChanged(object sender, RoutedEventArgs e)
        {
            _streamlabsWebApiConfig.StreamlabsSocketToken = StreamlabsSocketKeyInput.Password;
            SaveConfig();

            if (_streamlabsEventClient.IsConnected)
            {
                _streamlabsEventClient.Disconnect();
            }
        }

        private void TwitchFollowPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _streamlabsWebApiConfig.TwitchFollowPattern = TwitchFollowPattern.SelectedItem.ToString();
            SaveConfig();
        }

        private void TwitchFollowCyclesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Cycles = 0;
            if (int.TryParse(TwitchFollowCyclesInput.Text, out Cycles))
            {
                _streamlabsWebApiConfig.TwitchFollowPatternCycles = Cycles;
                SaveConfig();
            }
        }

        private void TwitchFollowSmokeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            float SmokeTime = 0;
            if (float.TryParse(TwitchFollowSmokeInput.Text, out SmokeTime))
            {
                _streamlabsWebApiConfig.TwitchFollowSmokeTime = SmokeTime;
                SaveConfig();
            }
        }


        private void TwitchSubPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _streamlabsWebApiConfig.TwitchSubPattern = TwitchSubPattern.SelectedItem.ToString();
            SaveConfig();
        }

        private void TwitchSubCyclesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Cycles = 0;
            if (int.TryParse(TwitchSubCyclesInput.Text, out Cycles))
            {
                _streamlabsWebApiConfig.TwitchSubPatternCycles = Cycles;
                SaveConfig();
            }
        }

        private void TwitchSubSmokeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            float SmokeTime = 0;
            if (float.TryParse(TwitchSubSmokeInput.Text, out SmokeTime))
            {
                _streamlabsWebApiConfig.TwitchSubSmokeTime = SmokeTime;
                SaveConfig();
            }
        }

        private void TwitchMysterySubPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _streamlabsWebApiConfig.TwitchMysterySubPattern = TwitchMysterySubPattern.SelectedItem.ToString();
            SaveConfig();
        }

        private void TwitchMysterySubCyclesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Cycles = 0;
            if (int.TryParse(TwitchMysterySubCyclesInput.Text, out Cycles))
            {
                _streamlabsWebApiConfig.TwitchMysterySubPatternCycles = Cycles;
                SaveConfig();
            }
        }

        private void TwitchMysterySubSmokeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            float SmokeTime = 0;
            if (float.TryParse(TwitchMysterySubSmokeInput.Text, out SmokeTime))
            {
                _streamlabsWebApiConfig.TwitchMysterySubSmokeTime = SmokeTime;
                SaveConfig();
            }
        }


        private void TwitchCheerPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _streamlabsWebApiConfig.TwitchCheerPattern = TwitchCheerPattern.SelectedItem.ToString();
            SaveConfig();
        }

        private void TwitchCheerCyclesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Cycles = 0;
            if (int.TryParse(TwitchCheerCyclesInput.Text, out Cycles))
            {
                _streamlabsWebApiConfig.TwitchCheerPatternCycles = Cycles;
                SaveConfig();
            }
        }

        private void TwitchCheerSmokeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            float SmokeTime = 0;
            if (float.TryParse(TwitchCheerSmokeInput.Text, out SmokeTime))
            {
                _streamlabsWebApiConfig.TwitchCheerSmokeTime = SmokeTime;
                SaveConfig();
            }
        }

        private void TwitchHostPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _streamlabsWebApiConfig.TwitchHostPattern = TwitchHostPattern.SelectedItem.ToString();
            SaveConfig();
        }

        private void TwitchHostCyclesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Cycles = 0;
            if (int.TryParse(TwitchHostCyclesInput.Text, out Cycles))
            {
                _streamlabsWebApiConfig.TwitchHostPatternCycles = Cycles;
                SaveConfig();
            }
        }

        private void TwitchHostSmokeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            float SmokeTime = 0;
            if (float.TryParse(TwitchHostSmokeInput.Text, out SmokeTime))
            {
                _streamlabsWebApiConfig.TwitchHostSmokeTime = SmokeTime;
                SaveConfig();
            }
        }


        private void TwitchRaidPattern_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _streamlabsWebApiConfig.TwitchRaidPattern = TwitchRaidPattern.SelectedItem.ToString();
            SaveConfig();
        }

        private void TwitchRaidCyclesInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Cycles = 0;
            if (int.TryParse(TwitchRaidCyclesInput.Text, out Cycles))
            {
                _streamlabsWebApiConfig.TwitchRaidPatternCycles = Cycles;
                SaveConfig();
            }
        }

        private void TwitchRaidSmokeInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            float SmokeTime = 0;
            if (float.TryParse(TwitchRaidSmokeInput.Text, out SmokeTime))
            {
                _streamlabsWebApiConfig.TwitchRaidSmokeTime = SmokeTime;
                SaveConfig();
            }
        }

        private void SlobsLedSceneNameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _slobsConfig.PatternSceneName = SlobsLedSceneNameInput.Text;
            SaveConfig();
        }

        private void SlobsLedSceneDurationInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Duration = 0;
            if (int.TryParse(SlobsLedSceneDurationInput.Text, out Duration))
            {
                _slobsConfig.PatternSceneDuration = Duration;
                SaveConfig();
            }
        }        

        private void NumericalInput_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void ObsLedSceneNameInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            _obsConfig.PatternSceneName = ObsLedSceneNameInput.Text;
            SaveConfig();
        }

        private void ObsLedSceneDurationInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            int Duration = 0;
            if (int.TryParse(ObsLedSceneDurationInput.Text, out Duration))
            {
                _obsConfig.PatternSceneDuration = Duration;
                SaveConfig();
            }
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
    }
}
