using System;

namespace CameraSlider.Bluetooth.Events
{
	public class CameraPositionChangedEventArgs : EventArgs
	{
		public enum PositionType
		{
			Slider,
			Pan,
			Tilt
		}

		public PositionType Type
		{
			get; set;
		}

		public int Value
		{
			get; set;
		}
	}
}