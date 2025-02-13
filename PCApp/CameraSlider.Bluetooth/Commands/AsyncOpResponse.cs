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

	public class AsyncOpResponse
	{
		public ResultCode Code { get; set; }
	};

	public class TypedAsyncOpResponse<T> : AsyncOpResponse
	{
		public T Data { get; set; }
	};
}
