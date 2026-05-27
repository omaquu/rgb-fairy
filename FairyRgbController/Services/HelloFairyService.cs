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

        // Continuous BLE advertisement watcher - same as Windows 11 Bluetooth settings
        private BluetoothLEAdvertisementWatcher? _watcher;
        private readonly List<BleDeviceInfo> _discovered = new();

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<List<BleDeviceInfo>>? DevicesUpdated;

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 15000)
        {
            // Stop previous scan
            StopWatcher();
            lock (_discovered) _discovered.Clear();

            NotifyStatus("Scanning for BLE devices...");

            // Use BluetoothLEAdvertisementWatcher for REAL continuous scanning
            // This is the EXACT same API Windows 11 Bluetooth settings uses
            // DeviceInformation.FindAllAsync is a one-shot query and misses intermittently-advertising devices
            _watcher = new BluetoothLEAdvertisementWatcher
            {
                ScanningMode = BluetoothLEScanningMode.Active
            };

            _watcher.Received += (watcher, args) =>
            {
                var name = args.Advertisement?.LocalName ?? string.Empty;
                var addr = args.BluetoothAddress.ToString("X12");
                var rssi = args.RawSignalStrengthInDBm;

                // Skip unnamed devices
                if (string.IsNullOrEmpty(name))
                    return;

                lock (_discovered)
                {
                    if (!_discovered.Any(d => d.BluetoothAddress == addr || d.Name == name))
                    {
                        var device = new BleDeviceInfo
                        {
                            Id = $"BLE:{addr}",
                            BluetoothAddress = addr,
                            Name = name,
                            Rssi = rssi,
                            IsPaired = false // unknown at watcher level
                        };
                        _discovered.Add(device);
                        var copy = new List<BleDeviceInfo>(_discovered);
                        DevicesUpdated?.Invoke(this, copy);
                        NotifyStatus($"Found: {name} (RSSI: {rssi} dBm) — {_discovered.Count} total");
                    }
                }
            };

            _watcher.Start();
            NotifyStatus($"Watcher started, listening for {timeoutMs / 1000}s...");

            await Task.Delay(timeoutMs);

            StopWatcher();

            // Also scan DeviceInformation for paired/classic devices
            try
            {
                var bleSel = BluetoothLEDevice.GetDeviceSelector();
                var classicSel = BluetoothDevice.GetDeviceSelector();
                var info = await DeviceInformation.FindAllAsync($"({bleSel}) OR ({classicSel})")
                    .AsTask().WaitAsync(TimeSpan.FromSeconds(5));
                foreach (var di in info)
                {
                    if (string.IsNullOrWhiteSpace(di.Name)) continue;
                    lock (_discovered)
                    {
                        if (!_discovered.Any(d => d.Id.EndsWith(di.Id) || d.Name == di.Name))
                        {
                            _discovered.Add(new BleDeviceInfo
                            {
                                Id = di.Id,
                                Name = di.Name,
                                IsPaired = IsDevicePaired(di)
                            });
                        }
                    }
                }
            }
            catch { }

            List<BleDeviceInfo> result;
            lock (_discovered) result = new List<BleDeviceInfo>(_discovered);
            NotifyStatus($"Complete: {result.Count} device(s)");
            DevicesUpdated?.Invoke(this, result);
            return result;
        }

        private void StopWatcher()
        {
            if (_watcher != null)
            {
                if (_watcher.Status == BluetoothLEAdvertisementWatcherStatus.Started)
                    _watcher.Stop();
                _watcher = null;
            }
        }

        private static bool IsDevicePaired(DeviceInformation di)
        {
            try { return di.Pairing?.IsPaired ?? false; }
            catch { return false; }
        }

        public async Task ConnectAsync(BleDeviceInfo deviceInfo)
        {
            if (_isConnected) await DisconnectAsync();
            NotifyStatus($"Connecting to {deviceInfo.Name}...");

            BluetoothLEDevice? device = null;

            // Try connecting by BluetoothAddress if available
            if (!string.IsNullOrEmpty(deviceInfo.BluetoothAddress))
            {
                ulong addr = Convert.ToUInt64(deviceInfo.BluetoothAddress, 16);
                device = await BluetoothLEDevice.FromBluetoothAddressAsync(addr);
            }

            // Fallback to DeviceInformation Id
            if (device == null && !string.IsNullOrEmpty(deviceInfo.Id) && deviceInfo.Id.StartsWith("BLE:"))
            {
                // Address from watcher - already tried above
            }
            else if (device == null)
            {
                device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            }

            if (device == null)
                throw new Exception($"Cannot connect to {deviceInfo.Name}");

            _device = device;
            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            for (int r = 0; r < 3; r++)
            {
                var gatt = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (gatt.Status == GattCommunicationStatus.Success)
                {
                    var svc = gatt.Services?.FirstOrDefault(s => s.Uuid == HelloFairyProtocol.ServiceUuid);
                    if (svc != null)
                    {
                        var chars = await svc.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                        if (chars.Status == GattCommunicationStatus.Success)
                        {
                            _commandCharacteristic = chars.Characteristics?.FirstOrDefault(c =>
                                c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
                            if (_commandCharacteristic != null)
                            {
                                _isConnected = true;
                                NotifyStatus($"✓ Connected to {deviceInfo.Name}");
                                return;
                            }
                        }
                    }
                }
                await Task.Delay(600 * (r + 1));
            }

            throw new Exception($"Hello Fairy service not found on {deviceInfo.Name}");
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
            await _commandCharacteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse);
        }

        public async Task SetPowerAsync(bool on)
        {
            await WriteCommandAsync(HelloFairyProtocol.BuildPowerCommand(on));
            NotifyStatus(on ? "Power ON" : "Power OFF");
        }

        public async Task SetHsvAsync(int h, int s, int v)
            => await WriteCommandAsync(HelloFairyProtocol.BuildColorCommand(h, s, v));

        public async Task SetPresetAsync(byte id, int b)
            => await WriteCommandAsync(HelloFairyProtocol.BuildPresetCommand(id, b));

        public Task TurnOffAsync() => SetPowerAsync(false);
    }
}