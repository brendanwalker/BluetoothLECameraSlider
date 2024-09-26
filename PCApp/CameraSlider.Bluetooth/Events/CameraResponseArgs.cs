using System;

namespace CameraSlider.Bluetooth.Events
{
	public class CameraStatusChangedEventArgs : EventArgs
	{
		public string Status
		{
			get; set;
		}
	}

	public class CameraResponseArgs : EventArgs
    {
        public string[] Args { get; set; }
    }

	public class CameraPositionValueChangedEventArgs : EventArgs
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