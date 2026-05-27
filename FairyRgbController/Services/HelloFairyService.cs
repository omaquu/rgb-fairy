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

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<List<BleDeviceInfo>>? DevicesUpdated;

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 60000)
        {
            var list = new List<BleDeviceInfo>();

            try
            {
                // Scan BOTH BLE and Classic Bluetooth devices
                var bleSelector = BluetoothLEDevice.GetDeviceSelector();
                var classicSelector = BluetoothDevice.GetDeviceSelector();
                var combinedSelector = $"({bleSelector}) OR ({classicSelector})";

                // 10 rounds of scanning, each round is a fresh FindAllAsync
                // BLE devices advertise asynchronously (2-10 second intervals)
                // More rounds = higher chance of catching the device
                // Total: up to 60 seconds (10 rounds x 5s each + 1s gaps)
                for (int round = 0; round < 10; round++)
                {
                    NotifyStatus($"Scanning round {round + 1}/10 ({list.Count} found)...");
                    var devices = await DeviceInformation.FindAllAsync(combinedSelector)
                        .AsTask().WaitAsync(TimeSpan.FromMilliseconds(5000));

                    int newCount = 0;
                    foreach (var di in devices)
                    {
                        if (string.IsNullOrWhiteSpace(di.Name)) continue;
                        if (!list.Any(d => d.Id == di.Id))
                            {
                                list.Add(new BleDeviceInfo
                                {
                                    Id = di.Id,
                                    Name = di.Name,
                                    IsPaired = IsDevicePaired(di),
                                    IsConnectable = di.IsEnabled
                                });
                                newCount++;
                        }
                    }

                    NotifyStatus($"Round {round + 1}: {newCount} new device(s) (total: {list.Count})");
                    DevicesUpdated?.Invoke(this, new List<BleDeviceInfo>(list));

                    if (list.Count > 0 && round >= 3) break; // Found devices, keep going a few more rounds
                    if (round < 9) await Task.Delay(1000);
                }

                NotifyStatus($"Scan complete: {list.Count} device(s)");
                DevicesUpdated?.Invoke(this, list);
            }
            catch (TimeoutException)
            {
                NotifyStatus("Scan timed out.");
            }
            catch (Exception ex)
            {
                NotifyStatus($"Scan failed: {ex.Message}");
            }

            return list;
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
            _device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            if (_device == null)
            {
                // Try as classic Bluetooth device
                var classic = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
                if (classic != null)
                    _device = await BluetoothLEDevice.FromBluetoothAddressAsync(classic.BluetoothAddress);
                if (_device == null)
                    throw new Exception($"Cannot connect to {deviceInfo.Name}");
            }

            _device.ConnectionStatusChanged += OnConnectionStatusChanged;

            for (int r = 0; r < 3; r++)
            {
                var gattResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (gattResult.Status == GattCommunicationStatus.Success)
                {
                    var service = gattResult.Services?.FirstOrDefault(s =>
                        s.Uuid == HelloFairyProtocol.ServiceUuid);
                    if (service != null)
                    {
                        var charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                        if (charResult.Status == GattCommunicationStatus.Success)
                        {
                            _commandCharacteristic = charResult.Characteristics?.FirstOrDefault(c =>
                                c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
                            if (_commandCharacteristic != null)
                            {
                                _isConnected = true;
                                NotifyStatus($"Connected to {deviceInfo.Name} ✓");
                                return;
                            }
                        }
                    }
                }
                await Task.Delay(500 * (r + 1));
            }

            throw new Exception($"Failed to find Hello Fairy service on {deviceInfo.Name}");
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

        private void NotifyStatus(string message)
        {
            StatusChanged?.Invoke(this, message);
        }
    }
}