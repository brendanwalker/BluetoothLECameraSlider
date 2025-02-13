using System.Configuration;

namespace CameraSlider.UI.Config
{
	public class WebSocketSection : ConfigurationSection
	{
		public WebSocketSection()
		{
			ServerPort = 8765;
		}

		[ConfigurationProperty("server_enabled")]
		public bool IsServerEnabled
		{
			get
			{
				return (bool)this["server_enabled"];
			}
			set
			{
				this["server_enabled"] = value;
			}
		}

		[ConfigurationProperty("server_port")]
		public int ServerPort
		{
			get
			{
				return (int)this["server_port"];
			}
			set
			{
				this["server_port"] = value;
			}
		}
	}
}
