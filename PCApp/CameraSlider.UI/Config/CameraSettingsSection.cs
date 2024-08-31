using System.Configuration;

namespace CameraSlider.UI.Config
{
	public class CameraSettingsSection : ConfigurationSection
	{
		public CameraSettingsSection()
		{
			SlidePos = 0.0f;
			PanPos = 0.0f;
			TiltPos = 0.0f;
			Speed = 0.05f;
			Accel = 0.05f;
			ManualSlideStepSize = 100;
			AutoSlideCalibration = true;
			AutoPanCalibration = true;
			AutoTiltCalibration = true;
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

		[ConfigurationProperty("speed")]
		public float Speed
		{
			get
			{
				return (float)this["speed"];
			}
			set
			{
				this["speed"] = value;
			}
		}

		[ConfigurationProperty("accel")]
		public float Accel
		{
			get
			{
				return (float)this["accel"];
			}
			set
			{
				this["accel"] = value;
			}
		}

		[ConfigurationProperty("manual_slide_step_size")]
		public int ManualSlideStepSize
		{
			get
			{
				return (int)this["manual_slide_step_size"];
			}
			set
			{
				this["manual_slide_step_size"] = value;
			}
		}

		[ConfigurationProperty("auto_slide_calibration")]
		public bool AutoSlideCalibration
		{
			get
			{
				return (bool)this["auto_slide_calibration"];
			}
			set
			{
				this["auto_slide_calibration"] = value;
			}
		}

		[ConfigurationProperty("auto_pan_calibration")]
		public bool AutoPanCalibration
		{
			get
			{
				return (bool)this["auto_pan_calibration"];
			}
			set
			{
				this["auto_pan_calibration"] = value;
			}
		}

		[ConfigurationProperty("auto_tilt_calibration")]
		public bool AutoTiltCalibration
		{
			get
			{
				return (bool)this["auto_tilt_calibration"];
			}
			set
			{
				this["auto_tilt_calibration"] = value;
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
}
