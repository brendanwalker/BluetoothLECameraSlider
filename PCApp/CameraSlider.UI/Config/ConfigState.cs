﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace CameraSlider.UI.Config
{
	public class ConfigState
	{
		public Configuration _configFile;
		public CameraSettingsSection _cameraSettingsConfig;
		public WebSocketSection _webSocketConfig;
		public List<PresetSettings> _presets = new List<PresetSettings>();
		public bool _areConfigSettingsDirty = false;
		public bool _arePresetsDirty = false;

		// Read-only config state from web socket server
		private string[] _presetNames = new string[] { };
		public string PresetNameListString
		{
			get {
				string result;

				lock (_presets)
				{
					result = string.Join(" ", _presetNames);
				}

				return result;
			}
		}
		
		public void LoadConfig()
		{
			// Open App.Config of executable
			string app_data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

			ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
			fileMap.ExeConfigFilename = app_data + "\\CameraSliderUI\\CameraSliderUI.config.txt";
			_configFile = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

			_presets = new List<PresetSettings>();
			_arePresetsDirty = false;
			try
			{
				_cameraSettingsConfig = (CameraSettingsSection)_configFile.GetSection("camera_settings");
			}
			catch (Exception)
			{
				_cameraSettingsConfig = null;
			}

			if (_cameraSettingsConfig != null)
			{
				if (_cameraSettingsConfig.PresetJson != "")
				{
					_presets = JsonConvert.DeserializeObject<List<PresetSettings>>(_cameraSettingsConfig.PresetJson);
					RebuildPresetNameList();
				}
			}
			else
			{
				_cameraSettingsConfig = new CameraSettingsSection();
				_configFile.Sections.Remove("camera_settings");
				_configFile.Sections.Add("camera_settings", _cameraSettingsConfig);
			}

			_webSocketConfig = (WebSocketSection)_configFile.GetSection("web_socket");
			if (_webSocketConfig == null)
			{
				_webSocketConfig = new WebSocketSection();
				_configFile.Sections.Add("web_socket", _webSocketConfig);
			}

			_configFile.Save();
		}

		public void SaveConfig()
		{
			if (_arePresetsDirty)
			{
				RebuildPresetNameList();
				_cameraSettingsConfig.PresetJson = JsonConvert.SerializeObject(_presets, Formatting.None);
				_arePresetsDirty = false;
			}
			_areConfigSettingsDirty = false;
			_configFile.Save();
		}

		private void RebuildPresetNameList()
		{
			lock(_presets)
			{
				_presetNames = _presets.Select(p => p.PresetName).ToArray();
			}
		}
	}
}
