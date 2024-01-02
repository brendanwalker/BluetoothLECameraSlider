using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraSlider.Bluetooth.Events
{
    public class DeviceRemovedEventArgs
    {
        public Schema.WatcherDevice Device { get; set; }
    }
}
