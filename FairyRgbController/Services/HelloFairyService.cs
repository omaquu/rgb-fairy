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

        // DeviceWatcher for continuous BLE scanning
        private DeviceWatcher? _deviceWatcher;
        private readonly List<BleDeviceInfo> _discoveredDevices = new();

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<List<BleDeviceInfo>>? DevicesUpdated;

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 10000)
        {
            _discoveredDevices.Clear();
            var completionSource = new TaskCompletionSource<bool>();

            // Fix: Use DeviceWatcher for continuous BLE scanning
            // Unlike FindAllAsync (one-shot), DeviceWatcher provides real-time device discovery
            // which catches devices that advertise intermittently
            var pairedSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
            var unpairedSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
            var combinedSelector = $"({pairedSelector}) OR ({unpairedSelector})";

            NotifyStatus("Scanning for BLE devices...");

            _deviceWatcher = DeviceInformation.CreateWatcher(
                combinedSelector,
                null, // no additional properties
                DeviceInformationKind.AssociationEndpoint);

            _deviceWatcher.Added += (watcher, deviceInfo) =>
            {
                if (!string.IsNullOrWhiteSpace(deviceInfo.Name))
                {
                    lock (_discoveredDevices)
                    {
                        if (!_discoveredDevices.Any(d => d.Id == deviceInfo.Id))
                        {
                            _discoveredDevices.Add(new BleDeviceInfo
                            {
                                Id = deviceInfo.Id,
                                Name = deviceInfo.Name,
                                IsPaired = IsDevicePaired(deviceInfo),
                                IsConnectable = deviceInfo.IsEnabled
                            });
                            // Notify UI with updated list
                            DevicesUpdated?.Invoke(this,
                                new List<BleDeviceInfo>(_discoveredDevices));
                        }
                    }
                }
            };

            _deviceWatcher.Updated += (watcher, deviceInfo, updateInfo) =>
            {
                // Update device info if needed
            };

            _deviceWatcher.Removed += (watcher, deviceInfo) =>
            {
                lock (_discoveredDevices)
                {
                    _discoveredDevices.RemoveAll(d => d.Id == deviceInfo.Id);
                    DevicesUpdated?.Invoke(this,
                        new List<BleDeviceInfo>(_discoveredDevices));
                }
            };

            _deviceWatcher.EnumerationCompleted += (watcher, args) =>
            {
                // Initial enumeration complete, but watcher continues to find new devices
                NotifyStatus($"Found {_discoveredDevices.Count} BLE device(s). Still listening...");
                completionSource.TrySetResult(true);
            };

            _deviceWatcher.Stopped += (watcher, args) =>
            {
                completionSource.TrySetResult(false);
            };

            _deviceWatcher.Start();

            // Wait for initial enumeration (timeoutMs)
            await Task.WhenAny(
                completionSource.Task,
                Task.Delay(timeoutMs));

            // Stop the watcher after timeout
            StopWatcher();

            NotifyStatus($"Scan complete. Found {_discoveredDevices.Count} device(s).");

            return new List<BleDeviceInfo>(_discoveredDevices);
        }

        private void StopWatcher()
        {
            if (_deviceWatcher != null &&
                (_deviceWatcher.Status == DeviceWatcherStatus.Started ||
                 _deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted))
            {
                try { _deviceWatcher.Stop(); } catch { }
            }
            _deviceWatcher = null;
        }

        private static bool IsDevicePaired(DeviceInformation deviceInfo)
        {
            try { return deviceInfo.Pairing?.IsPaired ?? false; }
            catch { return false; }
        }

        public async Task ConnectAsync(BleDeviceInfo deviceInfo)
        {
            if (_isConnected)
                await DisconnectAsync();

            NotifyStatus($"Connecting to {deviceInfo.Name}...");

            _device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            if (_device == null)
                throw new Exception($"Failed to get BluetoothLEDevice from Id: {deviceInfo.Id}");

            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Try GATT service discovery with retry
            GattServicesResult? gattServicesResult = null;
            for (int retry = 0; retry < 3; retry++)
            {
                gattServicesResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (gattServicesResult.Status == GattCommunicationStatus.Success)
                    break;
                await Task.Delay(500 * (retry + 1));
            }

            if (gattServicesResult == null || gattServicesResult.Status != GattCommunicationStatus.Success)
                throw new Exception($"Failed to get GATT services after 3 retries. " +
                    "Ensure device is powered on and in range.");

            var service = gattServicesResult.Services?.FirstOrDefault(s =>
                s.Uuid == HelloFairyProtocol.ServiceUuid);
            if (service == null)
            {
                var available = gattServicesResult.Services?.Select(s => s.Uuid.ToString()) ?? Enumerable.Empty<string>();
                throw new Exception(
                    $"Hello Fairy service not found. Expected: {HelloFairyProtocol.ServiceUuid}\n" +
                    $"Available: {(string.IsNullOrEmpty(string.Join(",", available)) ? "none" : string.Join(", ", available))}");
            }

            var characteristicsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (characteristicsResult.Status != GattCommunicationStatus.Success)
                throw new Exception($"Failed to get characteristics: {characteristicsResult.Status}");

            _commandCharacteristic = characteristicsResult.Characteristics?.FirstOrDefault(c =>
                c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
            if (_commandCharacteristic == null)
                throw new Exception("Command characteristic not found on Hello Fairy service.");

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
            StopWatcher();
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