using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraSlider.Bluetooth.Events
{
    public class CameraSliderEventArgs : EventArgs
    {
        public string Message { get; set; }
    }

	public class CameraIntValueChangedEventArgs : EventArgs
	{
		public int Value
		{
			get; set;
		}
	}
}