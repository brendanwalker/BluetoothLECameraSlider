namespace CameraSlider.UI.Config
{
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

		public PresetSettings()
		{
			PresetName = "";
			SlidePosition = 0;
			PanPosition = 0;
			TiltPosition = 0;
		}

		public PresetSettings(PresetSettings other)
		{
			PresetName = other.PresetName;
			SlidePosition = other.SlidePosition;
			PanPosition = other.PanPosition;
			TiltPosition = other.TiltPosition;
		}
	}
}
