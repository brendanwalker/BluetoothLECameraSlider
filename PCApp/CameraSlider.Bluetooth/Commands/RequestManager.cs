using CameraSlider.Bluetooth.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace CameraSlider.Bluetooth.Commands
{
	public class RequestManager
	{
		private Queue<Request> _pendingCommandQueue;
		public bool HasPendingRequests => _pendingCommandQueue.Count > 0;
		private Request _inFlightRequest= null;
		public bool HasInFlightRequest => _inFlightRequest != null;
		private int nextRequestId = 1;

		private Dictionary<int, AsyncOpFuture> _pendingAsyncOps;

		public RequestManager()
		{
			_pendingCommandQueue = new Queue<Request>();
			_pendingAsyncOps = new Dictionary<int, AsyncOpFuture>();
		}

		public AsyncOpFuture AddAsyncOp<T>(IAsyncOperation<T> asyncRequest)
		{
			AsyncOpFuture pendingRequest = null;

			lock (this)
			{
				pendingRequest= 
					new AsyncOpFuture(
						this, 
						nextRequestId, 
						 asyncRequest.AsTask());
				_pendingAsyncOps[nextRequestId] = pendingRequest;
				nextRequestId++;
			}

			return pendingRequest;
		}

		public AsyncOpFuture RemoveAsyncOp(int requestId)
		{
			AsyncOpFuture pendingRequest = null;

			lock (this)
			{
				if (_pendingAsyncOps.TryGetValue(requestId, out pendingRequest))
				{
					_pendingAsyncOps.Remove(requestId);
				}
			}

			return pendingRequest;
		}

		public ResultCode CancelAsyncOp(int requestId)
		{
			AsyncOpFuture existingRequest = RemoveAsyncOp(requestId);

			return existingRequest != null ? ResultCode.Success : ResultCode.InvalidParam;
		}

		public void EnqueueCommand(string message)
		{
			lock(this)
			{
				Request request = new Request
				{
					RequestId = nextRequestId,
					Message = message
				};
				nextRequestId++;

				_pendingCommandQueue.Enqueue(request);
			}
		}

		public bool TryDequeueRequestToSend(out Request request)
		{
			bool hasRequest = false;

			lock(this)
			{
				if (_pendingCommandQueue.Count > 0 && _inFlightRequest == null)
				{
					request = _pendingCommandQueue.Dequeue();
					
					// Mark the request as in-flight
					_inFlightRequest = request;

					hasRequest = true;
				}
				else
				{
					request = null;
				}
			}

			return hasRequest;
		}


		public void ClearInFlightCommand(Request request)
		{
			lock (this)
			{
				if (_inFlightRequest == request)
				{
					_inFlightRequest = null;
				}
			}
		}

		public CameraResponseArgs HandleCommandResponse(CameraResponseArgs response)
		{
			CameraResponseArgs processedResponse = null;

			if (response.Args.Length > 0 &&
				int.TryParse(response.Args[0], out int requestId))
			{
				lock(this)
				{
					if (_inFlightRequest != null && requestId == _inFlightRequest.RequestId)
					{
						processedResponse = new CameraResponseArgs
						{
							Args = response.Args.Skip(1).ToArray()
						};
					}

					_inFlightRequest = null;
				}
			}

			return processedResponse;
		}

		public void Flush()
		{
			lock(this)
			{
				_inFlightRequest = null;
				_pendingCommandQueue.Clear();
			}
		}
	}
}
