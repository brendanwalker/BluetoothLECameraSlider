using System;
using System.Threading.Tasks;
using Windows.Foundation;

namespace CameraSlider.Bluetooth.Commands
{
	public class AsyncOpFuture
	{
		private Task _task = null;

		public static AsyncOpFuture Create<T>(IAsyncOperation<T> asyncRequest)
		{
			return new AsyncOpFuture(asyncRequest.AsTask());
		}

		private AsyncOpFuture(Task future)
		{
			_task = future;
		}

		public bool IsValid()
		{
			return _task != null;
		}

		public bool IsCompleted()
		{
			return IsValid() && _task.IsCompleted;
		}

		// Non-Blocking Response Fetch
		// Return true if the response is ready, false otherwise
		public bool TryFetchResponse<T>(out T response)
		{
			response = default(T);

			if (IsCompleted())
			{
				Task<T> typedTask = _task as Task<T>;

				response = typedTask.Result;

				return true;
			}

			return false;
		}

		// Blocking Response Fetch
		// Return the response if it is ready, or a timeout response if the timeout is reached
		public TypedAsyncOpResponse<T> FetchResponse<T>(double timeoutMilliseconds = 1000)
		{
			TypedAsyncOpResponse<T> response;

			if (IsValid())
			{
				Task<T> typedTask = _task as Task<T>;

				if (timeoutMilliseconds > 0)
				{
					// Wait for a fixed timeout
					_task.Wait(TimeSpan.FromMilliseconds(timeoutMilliseconds));

					if (_task.IsCompleted && !_task.IsFaulted)
					{
						response = new TypedAsyncOpResponse<T> { 
							Code = ResultCode.Success,
							Data = typedTask.Result
						};
					}
					else
					{
						// Return a timeout response instead
						response = new TypedAsyncOpResponse<T>
						{
							Code = ResultCode.Timeout,
							Data = default(T)
						};
					}
				}
				else
				{
					// Wait indefinitely
					_task.Wait();

					// Return the result once the task succeeds or fails
					response = new TypedAsyncOpResponse<T>
					{
						Code = ResultCode.Success,
						Data = typedTask.Result
					};
				}
			}
			else
			{
				response = new TypedAsyncOpResponse<T>
				{
					Code = ResultCode.Uninitialized,
					Data = default(T)
				};
			}

			return response;
		}
	}
}
