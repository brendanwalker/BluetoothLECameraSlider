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
}
