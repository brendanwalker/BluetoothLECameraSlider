using CameraSlider.Bluetooth.Events;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Devices.Enumeration;

namespace CameraSlider.Bluetooth
{
	public class CameraSliderDevice
	{
		public readonly short MAX_SMOKE_DURATION_MILLIS = 10 * 1000; // Max 10 milliseconds

		public readonly string SERVICE_UUID = "6b42e290-28cb-11ee-be56-0242ac120002";
		public readonly string COMMAND_CHARACTERISTIC_UUID = "62c6b5d1-d304-4b9c-b12b-decd1e5b3614";
		public readonly string EVENT_CHARACTERISTIC_UUID = "5c88bae1-db64-4483-a0f3-6b6786c6c145";
		public readonly string SLIDER_POS_CHARACTERISTIC_UUID = "87b8f554-28cb-11ee-be56-0242ac120002";
		public readonly string SLIDER_SPEED_CHARACTERISTIC_UUID = "781ef5a1-e5df-411d-9276-7a229e469719";
		public readonly string SLIDER_ACCEL_CHARACTERISTIC_UUID = "5cf074e3-2014-4776-8734-b0d0eb49229a";
		public readonly string PAN_POS_CHARACTERISTIC_UUID = "a86268cb-73bb-497c-bb9b-cf7af318919f";
		public readonly string PAN_SPEED_CHARACTERISTIC_UUID = "838a79d5-c5f2-40ea-8f3d-c9d6fde08f56";
		public readonly string PAN_ACCEL_CHARACTERISTIC_UUID = "4049db30-2096-460a-821f-05380dc37212";
		public readonly string TILT_POS_CHARACTERISTIC_UUID = "9881b453-6636-4a73-a335-11bc737f6812";
		public readonly string TILT_SPEED_CHARACTERISTIC_UUID = "d40730f2-7be2-425a-a70c-58d56f8d2295";
		public readonly string TILT_ACCEL_CHARACTERISTIC_UUID = "885bee55-f069-4077-b18f-5cdb8b4a7003";

		private BluetoothLEDevice _cameraSliderDevice = null;

		private GattDeviceService _cameraSliderService = null;

		private GattCharacteristic _commandCharacteristic = null;
		private GattCharacteristic _eventCharacteristic = null;

		private GattCharacteristic _slidePosCharacteristic = null;
		private GattCharacteristic _slideSpeedCharacteristic = null;
		private GattCharacteristic _slideAccelCharacteristic = null;

		private GattCharacteristic _panPosCharacteristic = null;
		private GattCharacteristic _panSpeedCharacteristic = null;
		private GattCharacteristic _panAccelCharacteristic = null;

		private GattCharacteristic _tiltPosCharacteristic = null;
		private GattCharacteristic _tiltSpeedCharacteristic = null;
		private GattCharacteristic _tiltAccelCharacteristic = null;

		public event EventHandler<Events.ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
		protected virtual void OnConnectionStatusChanged(Events.ConnectionStatusChangedEventArgs e)
		{
			ConnectionStatusChanged?.Invoke(this, e);
		}

		public event EventHandler<Events.CameraSliderEventArgs> CameraSliderEventHandler;
		protected virtual void OnCameraSliderEvent(Events.CameraSliderEventArgs e)
		{
			CameraSliderEventHandler?.Invoke(this, e);
		}

		public async Task<bool> ConnectAsync(string deviceName)
		{
			// Get Bluetooth devices
			//string deviceSelector = BluetoothLEDevice.GetDeviceSelector();
			string deviceSelector= BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
			var devices = await DeviceInformation.FindAllAsync(deviceSelector);

			foreach (var device in devices)
			{
				if (device.Name == deviceName)
				{
					try
					{
						// Connect to the Bluetooth LE device
						_cameraSliderDevice = await BluetoothLEDevice.FromIdAsync(device.Id);
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

			if (await SetupCameraSliderCharacteristics() == false)
			{
				return false;
			}

			// we could force propagation of event with connection status change, to run the callback for initial status
			DeviceConnectionStatusChanged(_cameraSliderDevice, null);

			return true;
		}

		private async Task<bool> SetupCameraSliderCharacteristics()
		{
			_cameraSliderService = await GetDeviceServiceByUuidAsync(SERVICE_UUID);
			if (_cameraSliderService == null)
				return false;

			_commandCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, COMMAND_CHARACTERISTIC_UUID);
			if (_commandCharacteristic == null)
				return false;

			_eventCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, EVENT_CHARACTERISTIC_UUID);
			if (_eventCharacteristic == null)
				return false;

			await RegisterCharacteristicValueChangeCallback(_eventCharacteristic, NotifyCameraSliderEvent);

			_slidePosCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, SLIDER_POS_CHARACTERISTIC_UUID);
			if (_slidePosCharacteristic == null)
				return false;

			_slideSpeedCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, SLIDER_SPEED_CHARACTERISTIC_UUID);
			if (_slideSpeedCharacteristic == null)
				return false;

			_slideAccelCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, SLIDER_ACCEL_CHARACTERISTIC_UUID);
			if (_slideAccelCharacteristic == null)
				return false;

			_panPosCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, PAN_POS_CHARACTERISTIC_UUID);
			if (_panPosCharacteristic == null)
				return false;

			_panSpeedCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, PAN_SPEED_CHARACTERISTIC_UUID);
			if (_panSpeedCharacteristic == null)
				return false;

			_panAccelCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, PAN_ACCEL_CHARACTERISTIC_UUID);
			if (_panAccelCharacteristic == null)
				return false;

			_tiltPosCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, TILT_POS_CHARACTERISTIC_UUID);
			if (_tiltPosCharacteristic == null)
				return false;

			_tiltSpeedCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, TILT_SPEED_CHARACTERISTIC_UUID);
			if (_tiltSpeedCharacteristic == null)
				return false;

			_tiltAccelCharacteristic = await GetServiceCharacteristicByUuidAsync(_cameraSliderService, TILT_ACCEL_CHARACTERISTIC_UUID);
			if (_tiltAccelCharacteristic == null)
				return false;

			return true;
		}

		private async Task<GattDeviceService> GetDeviceServiceByUuidAsync(string serviceUuid)
		{
			// Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
			// BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
			// If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
			GattDeviceServicesResult result = await _cameraSliderDevice.GetGattServicesForUuidAsync(new Guid(serviceUuid), BluetoothCacheMode.Uncached);

			if (result.Status == GattCommunicationStatus.Success)
			{
				return result.Services.FirstOrDefault();
			}
			else
			{
				return null;
			}
		}

		private async Task<GattCharacteristic> GetServiceCharacteristicByUuidAsync(GattDeviceService service, string characteristicUuid)
		{
			GattCharacteristicsResult result = await service.GetCharacteristicsForUuidAsync(new Guid(characteristicUuid), BluetoothCacheMode.Uncached);

			if (result.Status == GattCommunicationStatus.Success)
			{
				return result.Characteristics.FirstOrDefault();
			}
			else
			{
				return null;
			}
		}

		private async Task<bool> RegisterCharacteristicValueChangeCallback(GattCharacteristic characteristic, TypedEventHandler<GattCharacteristic, GattValueChangedEventArgs> callback)
		{
			if (characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate))
			{
				var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Indicate);
				if (status == GattCommunicationStatus.Success)
				{
					characteristic.ValueChanged += callback;
				}

				return status == GattCommunicationStatus.Success;
			}
			else
			{
				return false;
			}
		}

		public async Task<bool> DisconnectAsync()
		{
			bool success = true;

			if (_cameraSliderDevice != null)
			{
				if (_eventCharacteristic != null)
				{
					try
					{
						GattCommunicationStatus status =
							await _eventCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
								GattClientCharacteristicConfigurationDescriptorValue.None);
						success = status == GattCommunicationStatus.Success;
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

				_commandCharacteristic = null;
				_eventCharacteristic = null;

				_slidePosCharacteristic = null;
				_slideSpeedCharacteristic = null;
				_slideAccelCharacteristic = null;

				_panPosCharacteristic = null;
				_panSpeedCharacteristic = null;
				_panAccelCharacteristic = null;

				_tiltPosCharacteristic = null;
				_tiltSpeedCharacteristic = null;
				_tiltAccelCharacteristic = null;

				_cameraSliderDevice.Dispose();
				_cameraSliderDevice = null;
			}
		}

		private async Task<bool> SendCommand(string command)
		{
			try
			{
				GattWriteResult result =
					await _commandCharacteristic.WriteValueWithResultAsync(
						CryptographicBuffer.ConvertStringToBinary(command, BinaryStringEncoding.Utf8));

				return result.Status == GattCommunicationStatus.Success;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private async Task<bool> SendFloat(GattCharacteristic characteristic, float value)
		{
			try
			{
				byte[] Message = BitConverter.GetBytes(value);
				GattWriteResult result = await characteristic.WriteValueWithResultAsync(Message.AsBuffer());

				return result.Status == GattCommunicationStatus.Success;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> SetSlidePosition(float position)
		{
			return await SendFloat(_slidePosCharacteristic, position);
		}

		public async Task<bool> SetPanPosition(float position)
		{
			return await SendFloat(_panPosCharacteristic, position);
		}

		public async Task<bool> SetTiltPosition(float position)
		{
			return await SendFloat(_tiltPosCharacteristic, position);
		}

		public async Task<bool> SetSlideSpeed(float speed)
		{
			return await SendFloat(_slideSpeedCharacteristic, speed);
		}

		public async Task<bool> SetPanSpeed(float speed)
		{
			return await SendFloat(_panSpeedCharacteristic, speed);
		}

		public async Task<bool> SetTiltSpeed(float speed)
		{
			return await SendFloat(_tiltSpeedCharacteristic, speed);
		}

		public async Task<bool> SetSlideAcceleration(float acceleration)
		{
			return await SendFloat(_slideAccelCharacteristic, acceleration);
		}

		public async Task<bool> SetPanAcceleration(float acceleration)
		{
			return await SendFloat(_panAccelCharacteristic, acceleration);
		}

		public async Task<bool> SetTiltAcceleration(float acceleration)
		{
			return await SendFloat(_tiltAccelCharacteristic, acceleration);
		}

		private async Task<float> GetFloat(GattCharacteristic characteristic)
		{
			try
			{
				GattReadResult result = await characteristic.ReadValueAsync();

				return BitConverter.ToSingle(result.Value.ToArray(), 0);
			}
			catch (Exception)
			{
				return 0.0f;
			}
		}

		public async Task<float> GetSlidePosition()
		{
			return await GetFloat(_slidePosCharacteristic);
		}

		public async Task<float> GetPanPosition()
		{
			return await GetFloat(_panPosCharacteristic);
		}

		public async Task<float> GetTiltPosition()
		{
			return await GetFloat(_tiltPosCharacteristic);
		}

		public async Task<float> GetSlideSpeed()
		{
			return await GetFloat(_slideSpeedCharacteristic);
		}

		public async Task<float> GetPanSpeed()
		{
			return await GetFloat(_panSpeedCharacteristic);
		}

		public async Task<float> GetTiltSpeed()
		{
			return await GetFloat(_tiltSpeedCharacteristic);
		}

		public async Task<float> GetSlideAcceleration()
		{
			return await GetFloat(_slideAccelCharacteristic);
		}

		public async Task<float> GetPanAcceleration()
		{
			return await GetFloat(_panAccelCharacteristic);
		}

		public async Task<float> GetTiltAcceleration()
		{
			return await GetFloat(_tiltAccelCharacteristic);
		}

		public async Task<bool> WakeUp()
		{
			return await SendCommand("ping");
		}

		public async Task<bool> StartCalibration()
		{
			return await SendCommand("calibrate");
		}

		public async Task<bool> StopAllMotors()
		{
			return await SendCommand("stop");
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

			OnConnectionStatusChanged(result);
		}

		private void NotifyCameraSliderEvent(GattCharacteristic sender, GattValueChangedEventArgs e)
		{
			string Result = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, e.CharacteristicValue);

			var args = new Events.CameraSliderEventArgs()
			{
				Message = Result
			};
			OnCameraSliderEvent(args);
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