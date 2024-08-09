using System.Configuration;

namespace CameraSlider.UI.Config
{
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
}
