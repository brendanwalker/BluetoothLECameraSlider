using System;
using System.Threading.Tasks;

namespace CameraSlider.Bluetooth.Commands
{
	public class AsyncOpFuture
	{
		private RequestManager _ownerRequestManager = null;
		private int _requestId = -1;
		private Task _task = null;

		public AsyncOpFuture()
		{
		}

		public AsyncOpFuture(
			RequestManager requestManager,
			int requestId,
			Task future)
		{
			_ownerRequestManager = requestManager;
			_requestId = requestId;
			_task = future;
		}

		public AsyncOpFuture(AsyncOpFuture other)
		{
			_requestId = other._requestId;
			_task = other._task;
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

				if (_ownerRequestManager != null && _requestId != -1)
				{
					_ownerRequestManager.RemoveAsyncOp(_requestId);
				}

				return true;
			}

			return false;
		}

		// Blocking Response Fetch
		// Return the response if it is ready, or a timeout response if the timeout is reached
		public TypedResponse<T> FetchResponse<T>(double timeoutMilliseconds = 1000)
		{
			TypedResponse<T> response;

			if (IsValid())
			{
				Task<T> typedTask = _task as Task<T>;

				if (timeoutMilliseconds > 0)
				{
					// Wait for a fixed timeout
					_task.Wait(TimeSpan.FromMilliseconds(timeoutMilliseconds));

					if (_task.IsCompleted && !_task.IsFaulted)
					{
						response = new TypedResponse<T> { 
							RequestId = _requestId,
							Code = ResultCode.Success,
							Data = typedTask.Result
						};
					}
					else
					{
						// Timeout reached, cancel the request
						if (_ownerRequestManager != null &&
							_requestId != -1)
						{
							_ownerRequestManager.CancelAsyncOp(_requestId);
						}

						// Return a timeout response instead
						response = new TypedResponse<T>
						{
							RequestId = _requestId,
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
					response = new TypedResponse<T>
					{
						RequestId = _requestId,
						Code = ResultCode.Success,
						Data = typedTask.Result
					};
				}
			}
			else
			{
				response = new TypedResponse<T>
				{
					RequestId = _requestId,
					Code = ResultCode.Uninitialized,
					Data = default(T)
				};
			}

			if (_ownerRequestManager != null && _requestId != -1)
			{
				_ownerRequestManager.RemoveAsyncOp(_requestId);
			}

			return response;
		}
	}
}
