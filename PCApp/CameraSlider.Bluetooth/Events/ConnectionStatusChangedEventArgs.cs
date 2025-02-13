using System;

namespace CameraSlider.Bluetooth.Events
{
    public class ConnectionStatusChangedEventArgs: EventArgs
    {
        public bool IsConnected { get; set; }
    }
}
