using CameraSlider.Bluetooth.Events;
using CameraSlider.Bluetooth.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Drawing;

namespace CameraSlider.Bluetooth
{
    public class DomeLightsDevice
    {
        public readonly short MAX_SMOKE_DURATION_MILLIS = 10 * 1000; // Max 10 milliseconds

        private BluetoothLEDevice _domeLightsDevice = null;
        private List<BluetoothAttribute> _serviceCollection = new List<BluetoothAttribute>();

        private BluetoothAttribute _domeLightReadAttribute;
        private BluetoothAttribute _domeLightWriteAttribute;
        private BluetoothAttribute _domeLightServiceAttribute;
        private GattCharacteristic _domeLightReadCharacteristic;
        private GattCharacteristic _domeLightWriteCharacteristic;

        /// <summary>
        /// Occurs when [connection status changed].
        /// </summary>
        public event EventHandler<Events.ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        /// <summary>
        /// Raises the <see cref="E:ConnectionStatusChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="Events.ConnectionStatusChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnConnectionStatusChanged(Events.ConnectionStatusChangedEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when [value changed].
        /// </summary>
        public event EventHandler<Events.UARTMessageEventArgs> UARTMessageHandler;
        /// <summary>
        /// Raises the <see cref="E:ValueChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="Events.UARTMessageEventArgs"/> instance containing the event data.</param>
        protected virtual void OnUARTMessage(Events.UARTMessageEventArgs e)
        {
            UARTMessageHandler?.Invoke(this, e);
        }

        public async Task<ConnectionResult> ConnectAsync(string deviceId)
        {
            _domeLightsDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (_domeLightsDevice == null)
            {
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = "Could not find specified dome lights device"
                };
            }

            if (!_domeLightsDevice.DeviceInformation.Pairing.IsPaired)
            {
                _domeLightsDevice = null;
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = "Dome Lights device is not paired"
                };
            }

            // we should always monitor the connection status
            _domeLightsDevice.ConnectionStatusChanged -= DeviceConnectionStatusChanged;
            _domeLightsDevice.ConnectionStatusChanged += DeviceConnectionStatusChanged;

            var isReachable = await GetDeviceServicesAsync();
            if (!isReachable)
            {
                _domeLightsDevice = null;
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = "Dome lights device is unreachable (i.e. out of range or shutoff)"
                };
            }

            CharacteristicResult characteristicResult;
            characteristicResult = await SetupDomeLightsCharacteristic();
            if (!characteristicResult.IsSuccess)
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = characteristicResult.Message
                };

            // we could force propagation of event with connection status change, to run the callback for initial status
            DeviceConnectionStatusChanged(_domeLightsDevice, null);

            return new Schema.ConnectionResult()
            {
                IsConnected = _domeLightsDevice.ConnectionStatus == BluetoothConnectionStatus.Connected,
                Name = _domeLightsDevice.Name
            };
        }

        private async Task<List<BluetoothAttribute>> GetServiceCharacteristicsAsync(BluetoothAttribute service)
        {
            IReadOnlyList<GattCharacteristic> characteristics = null;
            try
            {
                // Ensure we have access to the device.
                var accessStatus = await service.service.RequestAccessAsync();
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                    // and the new Async functions to get the characteristics of unpaired devices as well. 
                    var result = await service.service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        characteristics = result.Characteristics;
                    }
                    else
                    {
                        characteristics = new List<GattCharacteristic>();
                    }
                }
                else
                {
                    // Not granted access
                    // On error, act as if there are no characteristics.
                    characteristics = new List<GattCharacteristic>();
                }
            }
            catch (Exception)
            {
                characteristics = new List<GattCharacteristic>();
            }

            var characteristicCollection = new List<BluetoothAttribute>();
            characteristicCollection.AddRange(characteristics.Select(a => new BluetoothAttribute(a)));
            return characteristicCollection;
        }

        private async Task<CharacteristicResult> SetupDomeLightsCharacteristic()
        {
            _domeLightServiceAttribute = _serviceCollection.Where(a => a.Name == "Custom Service: 6e400001-b5a3-f393-e0a9-e50e24dcca9e").FirstOrDefault();
            if (_domeLightServiceAttribute == null)
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Cannot find DomeLights service"
                };
            }

            var characteristics = await GetServiceCharacteristicsAsync(_domeLightServiceAttribute);

            _domeLightReadAttribute = characteristics.Where(a => a.Name == "TXD").FirstOrDefault();
            if (_domeLightReadAttribute == null)
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Cannot find DomeLights Read characteristic"
                };
            }
            _domeLightReadCharacteristic = _domeLightReadAttribute.characteristic;

            _domeLightWriteAttribute = characteristics.Where(a => a.Name == "Custom Characteristic: 6e400002-b5a3-f393-e0a9-e50e24dcca9e").FirstOrDefault();
            if (_domeLightWriteAttribute == null)
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Cannot find DomeLights Write characteristic"
                };
            }
            _domeLightWriteCharacteristic = _domeLightWriteAttribute.characteristic;

            // Get all the child descriptors of a characteristics. Use the cache mode to specify uncached descriptors only 
            // and the new Async functions to get the descriptors of unpaired devices as well. 
            var result = await _domeLightReadCharacteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = result.Status.ToString()
                };
            }
            result = await _domeLightWriteCharacteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = result.Status.ToString()
                };
            }

            if (_domeLightReadCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                var status = await _domeLightReadCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                    _domeLightReadCharacteristic.ValueChanged += DomeLightsNewOutput;

                return new CharacteristicResult()
                {
                    IsSuccess = status == GattCommunicationStatus.Success,
                    Message = status.ToString()
                };
            }
            else
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "DomeLightsRead characteristic does not support notify"
                };

            }

        }

        private async Task<bool> GetDeviceServicesAsync()
        {
            // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
            // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
            // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
            GattDeviceServicesResult result = await _domeLightsDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

            if (result.Status == GattCommunicationStatus.Success)
            {
                _serviceCollection.AddRange(result.Services.Select(a => new BluetoothAttribute(a)));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnects the current BLE heart rate device.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DisconnectAsync()
        {
            bool success = true;

            if (_domeLightsDevice != null)
            {
                if (_domeLightReadCharacteristic != null)
                {
                    try
                    {
                        GattCommunicationStatus status= 
                            await _domeLightReadCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.None);
                        success = status == GattCommunicationStatus.Success;
                    }
                    catch(Exception)
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
            if (_domeLightsDevice != null)
            {
                if (_domeLightServiceAttribute != null)
                {
                    if (_domeLightServiceAttribute.service != null)
                        _domeLightServiceAttribute.service.Dispose();
                    _domeLightServiceAttribute = null;
                }

                _domeLightReadCharacteristic = null;
                _domeLightWriteCharacteristic = null;
                _domeLightReadAttribute = null;
                _domeLightWriteAttribute = null;

                _serviceCollection = new List<BluetoothAttribute>();
                _domeLightsDevice.Dispose();
                _domeLightsDevice = null;
            }
        }

        public async Task RequestVersionAsync()
        {
            byte[] Message = new byte[1] { Convert.ToByte('V') };
            await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());
        }

        public async Task CancelPatternAsync()
        {
            byte[] Message = new byte[1] { Convert.ToByte('C') };
            await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());
        }

        public enum Pattern { 
            NONE=0, 
            RAINBOW_CYCLE=1, 
            THEATER_CHASE=2, 
            COLOR_WIPE=3, 
            SCANNER=4, 
            FADE=5 };

        private byte GetClampedCycles(int Cycles)
        {
            return (byte)(Math.Min(Cycles, 255));
        }

        public async Task<bool> WakeUp()
        {
            try
            {
                byte[] Message = new byte[1] { Convert.ToByte('W') };
                GattWriteResult result= await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());

                return result.Status == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> PlayRainbowCycleAsync(int Cycles)
        {
            try
            {
                byte[] Message = new byte[3] {
                    Convert.ToByte('P'),
                    (byte)Pattern.RAINBOW_CYCLE,
                    GetClampedCycles(Cycles) };
                GattWriteResult result = await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());

                return result.Status == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> PlayTheaterChaseAsync(Color Color1, Color Color2, int Cycles)
        {
            try
            {
                byte[] Message = new byte[9] {
                    Convert.ToByte('P'),
                    (byte)Pattern.THEATER_CHASE,
                    Color1.R, Color1.G, Color1.B,
                    Color2.R, Color2.G, Color2.B,
                    GetClampedCycles(Cycles) };
                GattWriteResult result = await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());

                return result.Status == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> PlayColorWipeAsync(Color Color, int Cycles)
        {
            try
            {
                byte[] Message = new byte[6] {
                    Convert.ToByte('P'),
                    (byte)Pattern.COLOR_WIPE,
                    Color.R, Color.G, Color.B,
                    GetClampedCycles(Cycles) };
                GattWriteResult result = await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());

                return result.Status == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public async Task<bool> PlayScannerAsync(Color Color, int Cycles)
        {
            try
            {
                byte[] Message = new byte[6] {
                    Convert.ToByte('P'),
                    (byte)Pattern.SCANNER,
                    Color.R, Color.G, Color.B,
                    GetClampedCycles(Cycles) };
                GattWriteResult result = await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());

                return result.Status == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> PlayFadeAsync(Color Color1, Color Color2, int Steps, int Cycles)
        {
            try
            {
                byte[] Message = new byte[10] {
                    Convert.ToByte('P'),
                    (byte)Pattern.FADE,
                    Color1.R, Color1.G, Color1.B,
                    Color2.R, Color2.G, Color2.B,
                    (byte)Math.Min(Steps, 255),
                    GetClampedCycles(Cycles) };
                GattWriteResult result = await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());

                return result.Status == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> EmitSmokeAsync(short Milliseconds)
        {
            try
            {
                short ClampedMillis = Math.Min(Milliseconds, MAX_SMOKE_DURATION_MILLIS);

                byte[] Message = new byte[3] {
                    Convert.ToByte('S'),
                    (byte)(ClampedMillis >> 8),   // hi byte
                    (byte)(ClampedMillis & 255)}; // lo byte
                GattWriteResult result = await _domeLightWriteCharacteristic.WriteValueWithResultAsync(Message.AsBuffer());

                return result.Status == GattCommunicationStatus.Success;
            }
            catch (Exception)
            {
                return false;
            }
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

        private void DomeLightsNewOutput(GattCharacteristic sender, GattValueChangedEventArgs e)
        {
            string Result= CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, e.CharacteristicValue);

            var args = new Events.UARTMessageEventArgs()
            {
                Message = Result
            };
            OnUARTMessage(args);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get { return _domeLightsDevice != null ? _domeLightsDevice.ConnectionStatus == BluetoothConnectionStatus.Connected : false; }
        }

        /// <summary>
        /// Gets the device information for the current BLE heart rate device.
        /// </summary>
        /// <returns></returns>
        public async Task<Schema.DomeLightsDeviceInfo> GetDeviceInfoAsync()
        {
            if (_domeLightsDevice != null && _domeLightsDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                var deviceInfoService = _serviceCollection.Where(a => a.Name == "DeviceInformation").FirstOrDefault();
                var deviceInfocharacteristics = await GetServiceCharacteristicsAsync(deviceInfoService);

                //var batteryService = _serviceCollection.Where(a => a.Name == "Battery").FirstOrDefault();
                //var batteryCharacteristics = await GetServiceCharacteristicsAsync(batteryService);
                //byte battery = await _batteryParser.ReadAsync();

                return new Schema.DomeLightsDeviceInfo()
                {
                    DeviceId = _domeLightsDevice.DeviceId,
                    Name = _domeLightsDevice.Name,
                    Firmware = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "FirmwareRevisionString"),
                    Hardware = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "HardwareRevisionString"),
                    Manufacturer = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "ManufacturerNameString"),
                    SerialNumber = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "SerialNumberString"),
                    ModelNumber = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "ModelNumberString"),
                    //BatteryPercent = Convert.ToInt32(await Utilities.ReadCharacteristicValueAsync(batteryCharacteristics, "BatteryLevel"))
                };
            }
            else
            {
                return new Schema.DomeLightsDeviceInfo();
            }
        }
    }
}