using System;

namespace CameraSlider.Bluetooth.Events
{
    public class DeviceAddedEventArgs : EventArgs
    {
        public Schema.WatcherDevice Device { get; set; }
    }
}
