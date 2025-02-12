using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;

namespace CameraSlider.Bluetooth.Commands
{
	public enum ResultCode
	{
		Success,
		GeneralError,
		InvalidParam,
		Timeout,
		Uninitialized
	};

	public class Response
	{
		public int RequestId { get; set; }
		public ResultCode Code { get; set; }
	};

	public class TypedResponse<T> : Response
	{
		public T Data { get; set; }
	};
}
