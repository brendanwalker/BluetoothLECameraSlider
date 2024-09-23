using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

	public class CameraIntValueChangedEventArgs : EventArgs
	{
		public int Value
		{
			get; set;
		}
	}
}