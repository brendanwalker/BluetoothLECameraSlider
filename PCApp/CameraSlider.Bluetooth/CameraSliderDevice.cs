using CameraSlider.Bluetooth.Events;
using System;
using System.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Devices.Enumeration;
using System.Collections.Generic;
using CameraSlider.Bluetooth.Commands;

namespace CameraSlider.Bluetooth
{
	public class CameraSliderDevice
	{
		public readonly string SERVICE_UUID = "6b42e290-28cb-11ee-be56-0242ac120002";
		public readonly string STATUS_CHARACTERISTIC_UUID = "6b390b6c-b306-4ccb-a126-d759c5113177";
		public readonly string REQUEST_CHARACTERISTIC_UUID = "62c6b5d1-d304-4b9c-b12b-decd1e5b3614";
		public readonly string RESPONSE_CHARACTERISTIC_UUID = "5c88bae1-db64-4483-a0f3-6b6786c6c145";
		public readonly string SLIDER_POS_CHARACTERISTIC_UUID = "87b8f554-28cb-11ee-be56-0242ac120002";
		public readonly string PAN_POS_CHARACTERISTIC_UUID = "a86268cb-73bb-497c-bb9b-cf7af318919f";
		public readonly string TILT_POS_CHARACTERISTIC_UUID = "9881b453-6636-4a73-a335-11bc737f6812";
		public readonly string SPEED_CHARACTERISTIC_UUID = "781ef5a1-e5df-411d-9276-7a229e469719";
		public readonly string ACCEL_CHARACTERISTIC_UUID = "5cf074e3-2014-4776-8734-b0d0eb49229a";

		private BluetoothLEDevice _cameraSliderDevice = null;

		private GattDeviceService _cameraSliderService = null;

		private GattCharacteristic _statusCharacteristic = null;

		private GattCharacteristic _requestCharacteristic = null;
		private GattCharacteristic _responseCharacteristic = null;

		private GattCharacteristic _slidePosCharacteristic = null;
		private GattCharacteristic _panPosCharacteristic = null;
		private GattCharacteristic _tiltPosCharacteristic = null;

		private GattCharacteristic _speedCharacteristic = null;
		private GattCharacteristic _accelCharacteristic = null;

		private CommandManager _requestManager = new CommandManager();

		public event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
		public event EventHandler<CameraStatusChangedEventArgs> CameraStatusChanged;
		public event EventHandler<CameraResponseArgs> CameraResponseHandler;
		public event EventHandler<CameraPositionChangedEventArgs> SliderPosChanged;
		public event EventHandler<CameraPositionChangedEventArgs> PanPosChanged;
		public event EventHandler<CameraPositionChangedEventArgs> TiltPosChanged;

		public bool Connect(string deviceName)
		{
			// Get Bluetooth devices
			string deviceSelector= BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
			var deviceCollectionResponse= AsyncOpFuture.Create(
				DeviceInformation.FindAllAsync(deviceSelector))
				.FetchResponse<DeviceInformationCollection>();
			if (deviceCollectionResponse.Code != ResultCode.Success)
			{
				return false;
			}

			foreach (var device in deviceCollectionResponse.Data)
			{
				if (device.Name == deviceName)
				{
					try
					{
						// Connect to the Bluetooth LE device
						var deviceResponse= 
							AsyncOpFuture.Create(
								BluetoothLEDevice.FromIdAsync(device.Id))
							.FetchResponse<BluetoothLEDevice>();
						if (deviceResponse.Code == ResultCode.Success)
						{
							_cameraSliderDevice = deviceResponse.Data;
						}
						else
						{
							Console.WriteLine($"Error connecting to device, error code: {deviceResponse.Code}");
						}
					}
					catch (Exception ex)
					{
						// Handle exceptions
						Console.WriteLine($"Error connecting to device: {ex.Message}");
					}
					break;
				}
			}

			if (_cameraSliderDevice == null)
			{
				return false;
			}

			if (!_cameraSliderDevice.DeviceInformation.Pairing.IsPaired)
			{
				_cameraSliderDevice = null;
				return false;
			}

			// we should always monitor the connection status
			_cameraSliderDevice.ConnectionStatusChanged -= DeviceConnectionStatusChanged;
			_cameraSliderDevice.ConnectionStatusChanged += DeviceConnectionStatusChanged;

			if (SetupCameraSliderCharacteristics() == false)
			{
				return false;
			}

			// we could force propagation of event with connection status change, to run the callback for initial status
			DeviceConnectionStatusChanged(_cameraSliderDevice, null);

			return true;
		}

		private bool SetupCameraSliderCharacteristics()
		{
			_cameraSliderService = GetDeviceServiceByUuidAsync(SERVICE_UUID);
			if (_cameraSliderService == null)
				return false;

			_statusCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, STATUS_CHARACTERISTIC_UUID);
			if (_statusCharacteristic != null)
				RegisterCharacteristicValueChangeCallback(_statusCharacteristic, NotifyCameraStatusEvent);
			else
				return false;

			_requestCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, REQUEST_CHARACTERISTIC_UUID);
			if (_requestCharacteristic == null)
				return false;

			_responseCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, RESPONSE_CHARACTERISTIC_UUID);
			if (_responseCharacteristic != null)
				RegisterCharacteristicValueChangeCallback(_responseCharacteristic, NotifyCameraResponseEvent);
			else
				return false;

			_slidePosCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, SLIDER_POS_CHARACTERISTIC_UUID);
			if (_slidePosCharacteristic != null)
				RegisterCharacteristicValueChangeCallback(_slidePosCharacteristic, NotifySliderPosChangedEvent);
			else
				return false;

			_panPosCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, PAN_POS_CHARACTERISTIC_UUID);
			if (_panPosCharacteristic != null)
				RegisterCharacteristicValueChangeCallback(_panPosCharacteristic, NotifyPanPosChangedEvent);
			else
				return false;

			_tiltPosCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, TILT_POS_CHARACTERISTIC_UUID);
			if (_tiltPosCharacteristic != null)
				RegisterCharacteristicValueChangeCallback(_tiltPosCharacteristic, NotifyTiltPosChangedEvent);
			else
				return false;

			_speedCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, SPEED_CHARACTERISTIC_UUID);
			if (_speedCharacteristic == null)
				return false;

			_accelCharacteristic = GetServiceCharacteristicByUuidAsync(_cameraSliderService, ACCEL_CHARACTERISTIC_UUID);
			if (_accelCharacteristic == null)
				return false;

			return true;
		}

		private GattDeviceService GetDeviceServiceByUuidAsync(string serviceUuid)
		{
			// Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
			// BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
			// If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
			var response =
				AsyncOpFuture.Create(
					_cameraSliderDevice.GetGattServicesForUuidAsync(
						new Guid(serviceUuid), BluetoothCacheMode.Uncached))
				.FetchResponse<GattDeviceServicesResult>();

			if (response.Code == ResultCode.Success)
			{
				var result = response.Data;

				if (result.Status == GattCommunicationStatus.Success)
				{
					return result.Services.FirstOrDefault();
				}
			}

			return null;
		}

		private GattCharacteristic GetServiceCharacteristicByUuidAsync(GattDeviceService service, string characteristicUuid)
		{
			var response =
				AsyncOpFuture.Create(
					service.GetCharacteristicsForUuidAsync(
						new Guid(characteristicUuid), 
						BluetoothCacheMode.Uncached))
				.FetchResponse<GattCharacteristicsResult>();

			if (response.Code == ResultCode.Success)
			{
				var result = response.Data;

				if (result.Status == GattCommunicationStatus.Success)
				{
					return result.Characteristics.FirstOrDefault();
				}
			}

			return null;
		}

		private bool RegisterCharacteristicValueChangeCallback(GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback)
		{
			if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
			{
				var response =
					AsyncOpFuture.Create(
						characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
							GattClientCharacteristicConfigurationDescriptorValue.Notify))
					.FetchResponse<GattCommunicationStatus>();

				if (response.Code == ResultCode.Success)
				{
					if (response.Data == GattCommunicationStatus.Success)
					{
						characteristic.ValueChanged += callback;
					}

					return response.Data == GattCommunicationStatus.Success;
				}
			}

			return false;
		}

		public bool Disconnect()
		{
			bool success = true;

			if (_cameraSliderDevice != null)
			{
				if (_responseCharacteristic != null)
				{
					try
					{
						var response =
							AsyncOpFuture.Create(
								_responseCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
									GattClientCharacteristicConfigurationDescriptorValue.None))
							.FetchResponse<GattCommunicationStatus>();

						success = 
							response.Code == ResultCode.Success &&
							response.Data == GattCommunicationStatus.Success;
					}
					catch (Exception)
					{
						success = false;
					}
				}

				DeviceConnectionStatusChanged(null, null);
			}

			return success;
		}

		private void CleanUpGattState()
		{
			if (_cameraSliderDevice != null)
			{
				if (_cameraSliderService != null)
				{
					_cameraSliderService.Dispose();
					_cameraSliderService = null;
				}

				_statusCharacteristic = null;

				_requestCharacteristic = null;
				_responseCharacteristic = null;

				_slidePosCharacteristic = null;
				_panPosCharacteristic = null;
				_tiltPosCharacteristic = null;

				_speedCharacteristic = null;
				_accelCharacteristic = null;
			}
		}

		private void SendCommand(string command)
		{
			_requestManager.EnqueueCommand(command);

			// Try and send the command right away
			// No-ops if there is already a command in-flight
			SendNextPendingCommand();
		}

		private bool SendRequest(string command)
		{
			bool success= false;

			try
			{
				if (_requestCharacteristic != null)
				{
					var response =
						AsyncOpFuture.Create(
							_requestCharacteristic.WriteValueWithResultAsync(
								CryptographicBuffer.ConvertStringToBinary(command, BinaryStringEncoding.Utf8)))
						.FetchResponse<GattWriteResult>();
					if (response.Code == ResultCode.Success)
					{
						success = response.Data.Status == GattCommunicationStatus.Success;
					}
				}
			}
			catch (Exception)
			{
				success= false;
			}

			return success;
		}

		public void SendNextPendingCommand()
		{
			if (_requestManager.TryDequeueRequestToSend(out CommandRequest request))
			{
				string message = $"{request.RequestId} {request.Message}";

				if (!SendRequest(message))
				{
					_requestManager.ClearInFlightCommand(request);
				}
			}
		}

		public void SetPosition(float slide, float pan, float tilt)
		{
			string command = $"set_pos {slide} {pan} {tilt}";

			SendCommand(command);
		}

		private bool SendFloat(GattCharacteristic characteristic, float value)
		{
			bool success = false;

			try
			{
				if (characteristic != null)
				{
					byte[] Message = BitConverter.GetBytes(value);
					var response =
						AsyncOpFuture.Create(
							characteristic.WriteValueWithResultAsync(Message.AsBuffer()))
						.FetchResponse<GattWriteResult>();

					success =
						response.Code == ResultCode.Success &&
						response.Data.Status == GattCommunicationStatus.Success;
				}
			}
			catch (Exception)
			{
				success= false;
			}

			return success;
		}

		public bool SetSpeed(float speed)
		{
			return SendFloat(_speedCharacteristic, speed);
		}

		public bool SetAcceleration(float acceleration)
		{
			return SendFloat(_accelCharacteristic, acceleration);
		}

		private float GetFloat(GattCharacteristic characteristic)
		{
			float result= 0.0f;

			try
			{
				if (characteristic != null)
				{
					var response =
						AsyncOpFuture.Create(characteristic.ReadValueAsync())
						.FetchResponse<GattReadResult>();

					if (response.Code == ResultCode.Success)
					{
						result= BitConverter.ToSingle(response.Data.Value.ToArray(), 0);
					}
				}
			}
			catch (Exception)
			{
				result= 0.0f;
			}

			return result;
		}

		private int GetInt(GattCharacteristic characteristic)
		{
			int result= 0;

			try
			{
				if (characteristic == null)
				{
					var response =
						AsyncOpFuture.Create(characteristic.ReadValueAsync())
						.FetchResponse<GattReadResult>();

					if (response.Code == ResultCode.Success)
					{
						result = BitConverter.ToInt32(response.Data.Value.ToArray(), 0);
					}
				}
			}
			catch (Exception)
			{
				result= 0;
			}

			return result;
		}

		public int GetSlidePosition()
		{
			return GetInt(_slidePosCharacteristic);
		}

		public int GetPanPosition()
		{
			return GetInt(_panPosCharacteristic);
		}

		public int GetTiltPosition()
		{
			return GetInt(_tiltPosCharacteristic);
		}

		public float GetSpeed()
		{
			return GetFloat(_speedCharacteristic);
		}

		public float GetAcceleration()
		{
			return GetFloat(_accelCharacteristic);
		}

		public void GetSliderCalibration()
		{
			SendCommand("get_slider_calibration");
		}

		public void WakeUp()
		{
			SendCommand("ping");
		}

		public void ResetCalibration()
		{
			SendCommand("reset_calibration");
		}

		public void StartCalibration(bool slide, bool pan, bool tilt)
		{
			List<string> calibrateArgs = new List<string>();

			if (slide)
				calibrateArgs.Add("slide");
			if (pan)
				calibrateArgs.Add("pan");
			if (tilt)
				calibrateArgs.Add("tilt");

			string command = "calibrate " + string.Join(" ", calibrateArgs);

			SendCommand(command);
		}

		public void StopAllMotors()
		{
			SendCommand("stop");
		}

		public void MoveSlider(int delta)
		{
			string command = $"move_slider {delta}";

			SendCommand(command);
		}

		public void SetSlideMin()
		{
			SendCommand("set_slide_min_pos");
		}

		public void SetSlideMax()
		{
			SendCommand("set_slide_max_pos");
		}

		private void DeviceConnectionStatusChanged(BluetoothLEDevice sender, object args)
		{
			var result = new ConnectionStatusChangedEventArgs()
			{
				IsConnected = sender != null && (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
			};

			if (!result.IsConnected)
			{
				CleanUpGattState();
			}

			ConnectionStatusChanged?.Invoke(this, result);
		}

		private void NotifyCameraStatusEvent(GattCharacteristic sender, GattValueChangedEventArgs e)
		{
			string StatusString = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, e.CharacteristicValue);

			var StatusArgs = new Events.CameraStatusChangedEventArgs()
			{
				Status = StatusString
			};
			CameraStatusChanged?.Invoke(this, StatusArgs);
		}

		private void NotifyCameraResponseEvent(GattCharacteristic sender, GattValueChangedEventArgs e)
		{
			string Response = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, e.CharacteristicValue);

			var ResponseArgs = new CameraResponseArgs()
			{
				Args = Response.Split(' ')
			};

			var ProcessedArgs = _requestManager.HandleCommandResponse(ResponseArgs);
			if (ProcessedArgs != null)
			{
				CameraResponseHandler?.Invoke(this, ProcessedArgs);
			}
		}

		private CameraPositionChangedEventArgs CreatePositionChangedEvent(
			CameraPositionChangedEventArgs.PositionType type,
			GattValueChangedEventArgs e)
		{
			try
			{
				int result = BitConverter.ToInt32(e.CharacteristicValue.ToArray(), 0);

				var PositionChangeEvent = new CameraPositionChangedEventArgs()
				{
					Type = type,
					Value = result
				};

				return PositionChangeEvent;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private void NotifySliderPosChangedEvent(GattCharacteristic sender, GattValueChangedEventArgs e)
		{
			var Event = CreatePositionChangedEvent(
				CameraPositionChangedEventArgs.PositionType.Slider, e);
			if (Event != null)
				SliderPosChanged?.Invoke(this, Event);
		}

		private void NotifyPanPosChangedEvent(GattCharacteristic sender, GattValueChangedEventArgs e)
		{
			var Event = CreatePositionChangedEvent(
				CameraPositionChangedEventArgs.PositionType.Pan, e);
			if (Event != null)
				PanPosChanged?.Invoke(this, Event);
		}

		private void NotifyTiltPosChangedEvent(GattCharacteristic sender, GattValueChangedEventArgs e)
		{
			var Event = CreatePositionChangedEvent(
				CameraPositionChangedEventArgs.PositionType.Tilt, e);
			if (Event != null)
				TiltPosChanged?.Invoke(this, Event);
		}

		public bool IsConnected
		{
			get
			{
				return _cameraSliderDevice != null ? _cameraSliderDevice.ConnectionStatus == BluetoothConnectionStatus.Connected : false;
			}
		}
	}
}