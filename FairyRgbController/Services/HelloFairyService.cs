using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;
using FairyRgbController.Models;

namespace FairyRgbController.Services
{
    public class HelloFairyService : IFairyLedService
    {
        private BluetoothLEDevice? _device;
        private GattCharacteristic? _commandCharacteristic;
        private bool _isConnected;
        private string? _connectedDeviceId;

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<List<BleDeviceInfo>>? DevicesUpdated;

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 10000)
        {
            var list = new List<BleDeviceInfo>();

            try
            {
                // Fix: Scan BOTH paired AND unpaired BLE devices
                // The original code only scanned unpaired (GetDeviceSelectorFromPairingState(false))
                // If Windows has already paired/remembered the fairy device, it won't appear
                var pairedSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
                var unpairedSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
                var combinedSelector = $"({pairedSelector}) OR ({unpairedSelector})";

                NotifyStatus("Scanning for BLE devices...");
                var devices = await DeviceInformation.FindAllAsync(combinedSelector)
                    .AsTask().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));

                foreach (var deviceInfo in devices)
                {
                    // Skip devices with no name
                    if (string.IsNullOrWhiteSpace(deviceInfo.Name))
                        continue;

                    // Check if it looks like a fairy device (HM-10 / LEDnet / generic BLE)
                    var device = new BleDeviceInfo
                    {
                        Id = deviceInfo.Id,
                        Name = deviceInfo.Name,
                        IsConnectable = deviceInfo.IsEnabled,
                        IsPaired = IsDevicePaired(deviceInfo),
                        Rssi = 0 // RSSI not available from DeviceInformation
                    };
                    list.Add(device);
                }

                // Try to read RSSI for each device (async)
                // Note: RSSI is only available from watcher, not static enumeration
                // We'll mark device count instead

                NotifyStatus($"Found {list.Count} BLE device(s). Select one and connect.");
            }
            catch (TimeoutException)
            {
                NotifyStatus("Scan timed out. Try again.");
            }
            catch (Exception ex)
            {
                NotifyStatus($"Scan failed: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"BLE Scan error: {ex}");
            }

            return list;
        }

        private static bool IsDevicePaired(DeviceInformation deviceInfo)
        {
            try
            {
                return deviceInfo.Pairing?.IsPaired ?? false;
            }
            catch
            {
                return false;
            }
        }

        public async Task ConnectAsync(BleDeviceInfo deviceInfo)
        {
            if (_isConnected)
                await DisconnectAsync();

            NotifyStatus($"Connecting to {deviceInfo.Name}...");

            _device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            if (_device == null)
                throw new Exception($"Failed to get BluetoothLEDevice from Id: {deviceInfo.Id}");

            // Wait for connection
            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            var gattServicesResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (gattServicesResult.Status != GattCommunicationStatus.Success &&
                gattServicesResult.Status != GattCommunicationStatus.ProtocolError)
            {
                // Retry once for protocol error
                await Task.Delay(500);
                var retryResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (retryResult.Status != GattCommunicationStatus.Success)
                {
                    var errorMsg = retryResult.Status == GattCommunicationStatus.ProtocolError
                        ? $"GATT protocol error (try pairing in Windows Bluetooth settings first)"
                        : $"Failed to get services: {retryResult.Status}";
                    throw new Exception(errorMsg);
                }
            }

            var service = gattServicesResult.Services?.FirstOrDefault(s =>
                s.Uuid == HelloFairyProtocol.ServiceUuid);
            if (service == null)
            {
                // Try to enumerate all available services for debugging
                var serviceList = gattServicesResult.Services?.Select(s => s.Uuid.ToString()) ?? Enumerable.Empty<string>();
                var availableServices = string.Join(", ", serviceList);
                throw new Exception(
                    $"Hello Fairy service not found. Expected: {HelloFairyProtocol.ServiceUuid}\n" +
                    $"Available services: {(string.IsNullOrEmpty(availableServices) ? "none" : availableServices)}\n" +
                    "Try: 1) Ensure device is powered on 2) Unpair and re-pair in Windows Bluetooth settings");
            }

            var characteristicsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (characteristicsResult.Status != GattCommunicationStatus.Success)
                throw new Exception($"Failed to get characteristics: {characteristicsResult.Status}");

            _commandCharacteristic = characteristicsResult.Characteristics?.FirstOrDefault(c =>
                c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
            if (_commandCharacteristic == null)
                throw new Exception("Command characteristic not found on the Hello Fairy service.");

            _isConnected = true;
            _connectedDeviceId = deviceInfo.Id;
            NotifyStatus($"Connected to {deviceInfo.Name} ✓");
        }

        private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                _isConnected = false;
                _connectedDeviceId = null;
                NotifyStatus("Device disconnected.");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_device != null)
            {
                _device.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _device.Dispose();
                _device = null;
            }
            _commandCharacteristic = null;
            _isConnected = false;
            _connectedDeviceId = null;
            NotifyStatus("Disconnected.");
            await Task.CompletedTask;
        }

        private async Task WriteCommandAsync(byte[] packet)
        {
            if (!_isConnected || _commandCharacteristic == null)
                throw new InvalidOperationException("Not connected to device.");

            var writer = new DataWriter();
            writer.WriteBytes(packet);
            var buffer = writer.DetachBuffer();

            var result = await _commandCharacteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithResponse);
            if (result != GattCommunicationStatus.Success)
                throw new Exception($"Write failed: {result}");
        }

        public async Task SetPowerAsync(bool on)
        {
            var packet = HelloFairyProtocol.BuildPowerCommand(on);
            await WriteCommandAsync(packet);
            NotifyStatus(on ? "Power ON" : "Power OFF");
        }

        public async Task SetHsvAsync(int hue, int saturation, int value)
        {
            var packet = HelloFairyProtocol.BuildColorCommand(hue, saturation, value);
            await WriteCommandAsync(packet);
        }

        public async Task SetPresetAsync(byte presetId, int brightness)
        {
            var packet = HelloFairyProtocol.BuildPresetCommand(presetId, brightness);
            await WriteCommandAsync(packet);
        }

        public Task TurnOffAsync() => SetPowerAsync(false);

        private void NotifyStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
            System.Diagnostics.Debug.WriteLine($"[FairyService] {message}");
        }
    }
}