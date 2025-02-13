using CameraSlider.Bluetooth.Events;
using System.Collections.Generic;
using System.Linq;

namespace CameraSlider.Bluetooth.Commands
{
	public class CommandManager
	{
		private Queue<CommandRequest> _pendingCommandQueue;
		public bool HasPendingRequests => _pendingCommandQueue.Count > 0;
		private CommandRequest _inFlightRequest= null;
		public bool HasInFlightRequest => _inFlightRequest != null;
		private int nextRequestId = 1;

		public CommandManager()
		{
			_pendingCommandQueue = new Queue<CommandRequest>();
		}

		public void EnqueueCommand(string message)
		{
			lock(this)
			{
				CommandRequest request = new CommandRequest
				{
					RequestId = nextRequestId,
					Message = message
				};
				nextRequestId++;

				_pendingCommandQueue.Enqueue(request);
			}
		}

		public bool TryDequeueRequestToSend(out CommandRequest request)
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


		public void ClearInFlightCommand(CommandRequest request)
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
