using CameraSlider.Bluetooth.Events;
using System.Collections.Generic;
using System.Linq;

namespace CameraSlider.Bluetooth.Commands
{
	internal class RequestManager
	{
		private Queue<Request> _pendingRequests;
		public bool HasPendingRequests => _pendingRequests.Count > 0;
		private Request _inFlightRequest= null;
		public bool HasInFlightRequest => _inFlightRequest != null;
		private int nextRequestId = 1;

		public RequestManager()
		{
			_pendingRequests = new Queue<Request>();
		}

		public void EnqueueRequest(string message)
		{
			lock(this)
			{
				Request request = new Request
				{
					RequestId = nextRequestId,
					Message = message
				};
				nextRequestId++;

				_pendingRequests.Enqueue(request);
			}
		}

		public bool TryDequeueRequestToSend(out Request request)
		{
			bool hasRequest = false;

			lock(this)
			{
				if (_pendingRequests.Count > 0 && _inFlightRequest == null)
				{
					request = _pendingRequests.Dequeue();
					
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


		public void ClearInFlightRequest(Request request)
		{
			lock (this)
			{
				if (_inFlightRequest == request)
				{
					_inFlightRequest = null;
				}
			}
		}

		public CameraResponseArgs HandleResponse(CameraResponseArgs response)
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
				_pendingRequests.Clear();
			}
		}
	}
}
