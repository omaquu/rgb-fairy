using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Bluetooth.Advertisement;
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

        // Advertisement watcher for continuous BLE scanning
        private BluetoothLEAdvertisementWatcher? _adWatcher;
        private readonly List<BleDeviceInfo> _discoveredDevices = new();
        private readonly object _lock = new();

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<List<BleDeviceInfo>>? DevicesUpdated;

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 15000)
        {
            lock (_lock) _discoveredDevices.Clear();
            StopWatcher();

            NotifyStatus("Scanning for Bluetooth devices...");

            // Step 1: Use BluetoothLEAdvertisementWatcher for real-time BLE discovery
            // This is the same API Windows 11 uses for BLE scanning
            _adWatcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active // Active mode = request scan responses (gets device names)
            };

            _adWatcher.Received += (watcher, args) =>
            {
                string deviceName = string.Empty;
                try { deviceName = args.Advertisement.LocalName ?? string.Empty; } catch { }

                string bluetoothAddress = args.BluetoothAddress.ToString("X12");
                string displayName = string.IsNullOrEmpty(deviceName)
                    ? $"Unknown ({bluetoothAddress})"
                    : deviceName;

                lock (_lock)
                {
                    if (!_discoveredDevices.Any(d =>
                        d.Id == bluetoothAddress || d.Name == deviceName && !string.IsNullOrEmpty(deviceName)))
                    {
                        _discoveredDevices.Add(new BleDeviceInfo
                        {
                            Id = bluetoothAddress,
                            Name = displayName,
                            IsPaired = false // We'll check pairing separately
                        });
                        DevicesUpdated?.Invoke(this, new List<BleDeviceInfo>(_discoveredDevices));
                        NotifyStatus($"Found: {displayName} ({_discoveredDevices.Count} total)");
                    }
                }
            };

            _adWatcher.Start();

            // Step 2: Also do DeviceInformation.FindAllAsync for paired devices
            // Run in parallel with the watcher
            await Task.Delay(3000); // Let watcher run for 3 seconds first

            try
            {
                var bleSelector = BluetoothLEDevice.GetDeviceSelector();
                var classicSelector = BluetoothDevice.GetDeviceSelector();
                var combinedSelector = $"({bleSelector}) OR ({classicSelector})";

                var devices = await DeviceInformation.FindAllAsync(combinedSelector)
                    .AsTask().WaitAsync(TimeSpan.FromMilliseconds(10000));

                foreach (var di in devices)
                {
                    if (string.IsNullOrWhiteSpace(di.Name)) continue;
                    lock (_lock)
                    {
                        if (!_discoveredDevices.Any(d => d.Id == di.Id))
                        {
                            _discoveredDevices.Add(new BleDeviceInfo
                            {
                                Id = di.Id,
                                Name = di.Name,
                                IsPaired = IsDevicePaired(di),
                                IsConnectable = di.IsEnabled
                            });
                            DevicesUpdated?.Invoke(this, new List<BleDeviceInfo>(_discoveredDevices));
                            NotifyStatus($"Found: {di.Name} ({_discoveredDevices.Count} total)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NotifyStatus($"Enumeration error: {ex.Message}");
            }

            // Keep watcher running for remaining time
            int elapsed = 3000 + 10000; // initial delay + FindAllAsync
            int remaining = Math.Max(0, timeoutMs - elapsed);
            if (remaining > 0)
                await Task.Delay(remaining);

            StopWatcher();

            var result = new List<BleDeviceInfo>();
            lock (_lock) result.AddRange(_discoveredDevices);

            NotifyStatus($"Scan complete: {result.Count} device(s)");
            DevicesUpdated?.Invoke(this, result);
            return result;
        }

        private void StopWatcher()
        {
            if (_adWatcher != null)
            {
                try
                {
                    if (_adWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
                        _adWatcher.Stop();
                }
                catch { }
                _adWatcher = null;
            }
        }

        private static bool IsDevicePaired(DeviceInformation di)
        {
            try { return di.Pairing?.IsPaired ?? false; }
            catch { return false; }
        }

        public async Task ConnectAsync(BleDeviceInfo deviceInfo)
        {
            if (_isConnected)
                await DisconnectAsync();

            NotifyStatus($"Connecting to {deviceInfo.Name}...");

            // Try to get the device - if we have a DeviceInformation Id, use it;
            // if we have a BluetoothAddress (from watcher), convert it
            if (deviceInfo.Id.Length == 12 && deviceInfo.Id.All(c => "0123456789ABCDEF".Contains(c)))
            {
                // This is a Bluetooth address from the watcher - try both LE and Classic
                ulong address = Convert.ToUInt64(deviceInfo.Id, 16);

                _device = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
                if (_device == null)
                {
                    var classicDevice = await BluetoothDevice.FromBluetoothAddressAsync(address);
                    if (classicDevice != null)
                    {
                        // For classic BT devices we can't use GATT, try getting BLE device from Id
                        NotifyStatus("Classic BT device found, trying LE connection...");
                        _device = await BluetoothLEDevice.FromIdAsync(classicDevice.DeviceId);
                    }
                }
            }
            else
            {
                _device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            }

            if (_device == null)
                throw new Exception($"Could not connect to {deviceInfo.Name}");

            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            GattServicesResult? gattResult = null;
            for (int retry = 0; retry < 3; retry++)
            {
                gattResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (gattResult.Status == GattCommunicationStatus.Success)
                    break;
                await Task.Delay(500 * (retry + 1));
            }

            if (gattResult == null || gattResult.Status != GattCommunicationStatus.Success)
                throw new Exception("Failed to get GATT services.");

            var service = gattResult.Services?.FirstOrDefault(s =>
                s.Uuid == HelloFairyProtocol.ServiceUuid);
            if (service == null)
            {
                var available = gattResult.Services?
                    .Select(s => s.Uuid.ToString()) ?? Enumerable.Empty<string>();
                throw new Exception(
                    $"Service not found. Expected {HelloFairyProtocol.ServiceUuid}. " +
                    $"Available: {string.Join(", ", available)}");
            }

            var charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (charResult.Status != GattCommunicationStatus.Success)
                throw new Exception($"GetCharacteristics failed: {charResult.Status}");

            _commandCharacteristic = charResult.Characteristics?.FirstOrDefault(c =>
                c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
            if (_commandCharacteristic == null)
                throw new Exception("Command characteristic not found.");

            _isConnected = true;
            NotifyStatus($"Connected to {deviceInfo.Name} ✓");
        }

        private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                _isConnected = false;
                NotifyStatus("Disconnected.");
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
            NotifyStatus("Disconnected.");
            await Task.CompletedTask;
        }

        private async Task WriteCommandAsync(byte[] packet)
        {
            if (!_isConnected || _commandCharacteristic == null)
                throw new InvalidOperationException("Not connected.");

            var writer = new DataWriter();
            writer.WriteBytes(packet);
            var buffer = writer.DetachBuffer();
            var result = await _commandCharacteristic.WriteValueAsync(buffer, GattWriteOption.WriteWithResponse);
            if (result != GattCommunicationStatus.Success)
                throw new Exception($"Write failed: {result}");
        }

        public async Task SetPowerAsync(bool on)
        {
            await WriteCommandAsync(HelloFairyProtocol.BuildPowerCommand(on));
            NotifyStatus(on ? "Power ON" : "Power OFF");
        }

        public async Task SetHsvAsync(int hue, int saturation, int value)
        {
            await WriteCommandAsync(HelloFairyProtocol.BuildColorCommand(hue, saturation, value));
        }

        public async Task SetPresetAsync(byte presetId, int brightness)
        {
            await WriteCommandAsync(HelloFairyProtocol.BuildPresetCommand(presetId, brightness));
        }

        public Task TurnOffAsync() => SetPowerAsync(false);

        private void NotifyStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
            System.Diagnostics.Debug.WriteLine($"[FairyService] {message}");
        }
    }
}