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
        private BleDeviceInfo? _pendingAutoConnect;

        public string? ConnectedDeviceName { get; private set; }

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<List<BleDeviceInfo>>? DevicesUpdated;
        public event EventHandler<BleDeviceInfo>? AutoConnected;

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 30000)
        {
            var allDevices = new List<BleDeviceInfo>();
            _pendingAutoConnect = null;

            try
            {
                var bPaired = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
                var bUnpaired = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
                var classic = BluetoothDevice.GetDeviceSelector();
                var allSelector = $"({bPaired}) OR ({bUnpaired}) OR ({classic})";

                for (int round = 0; round < 15; round++)
                {
                    NotifyStatus($"Scan {round + 1}/15 ({allDevices.Count} found)...");
                    var devices = await DeviceInformation.FindAllAsync(allSelector)
                        .AsTask().WaitAsync(TimeSpan.FromMilliseconds(5000));

                    // Add new devices
                    foreach (var di in devices)
                    {
                        if (string.IsNullOrWhiteSpace(di.Name)) continue;
                        if (!allDevices.Any(d => d.Id == di.Id))
                        {
                            allDevices.Add(new BleDeviceInfo
                            {
                                Id = di.Id,
                                Name = di.Name,
                                IsPaired = IsPaired(di),
                                IsConnectable = di.IsEnabled
                            });
                        }
                    }

                    // Filter to only FAIRY devices for the UI
                    var fairyDevices = allDevices
                        .Where(d => d.Name.IndexOf("fairy", StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    NotifyStatus($"Round {round + 1}: {fairyDevices.Count} Fairy device(s)");
                    
                    // Only report Fairy devices to the UI
                    DevicesUpdated?.Invoke(this, fairyDevices);

                    // Auto-connect if exactly one Fairy device found and not already connected
                    if (fairyDevices.Count == 1 && !_isConnected && _pendingAutoConnect == null)
                    {
                        _pendingAutoConnect = fairyDevices[0];
                        NotifyStatus($"✓ Found {fairyDevices[0].Name} — connecting...");
                    }
                    else if (fairyDevices.Count > 1)
                    {
                        NotifyStatus($"{fairyDevices.Count} Fairy devices — select one");
                    }

                    if (round < 14) await Task.Delay(1000);
                }

                NotifyStatus("Scan complete");
            }
            catch (Exception ex)
            {
                NotifyStatus($"Scan error: {ex.Message}");
            }

            // Auto-connect after scan if we found one
            if (_pendingAutoConnect != null && !_isConnected)
            {
                var device = _pendingAutoConnect;
                _pendingAutoConnect = null;
                try
                {
                    await ConnectAsync(device);
                }
                catch (Exception ex)
                {
                    NotifyStatus($"Auto-connect failed: {ex.Message}");
                }
            }

            // Return only Fairy devices
            return allDevices
                .Where(d => d.Name.IndexOf("fairy", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private static bool IsPaired(DeviceInformation di)
        {
            try { return di.Pairing?.IsPaired ?? false; }
            catch { return false; }
        }

        public async Task ConnectAsync(BleDeviceInfo deviceInfo)
        {
            if (_isConnected) await DisconnectAsync();
            NotifyStatus($"Connecting to {deviceInfo.Name}...");

            _device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            if (_device == null)
            {
                var cd = await BluetoothDevice.FromIdAsync(deviceInfo.Id);
                if (cd != null)
                    _device = await BluetoothLEDevice.FromBluetoothAddressAsync(cd.BluetoothAddress);
                if (_device == null)
                    throw new Exception($"Cannot connect to {deviceInfo.Name}");
            }

            _device.ConnectionStatusChanged += OnDisconnect;

            for (int r = 0; r < 3; r++)
            {
                var gatt = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                if (gatt.Status == GattCommunicationStatus.Success)
                {
                    var svc = gatt.Services?.FirstOrDefault(s => s.Uuid == HelloFairyProtocol.ServiceUuid);
                    if (svc != null)
                    {
                        var chr = await svc.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                        _commandCharacteristic = chr.Characteristics?.FirstOrDefault(c =>
                            c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
                        if (_commandCharacteristic != null)
                        {
                            _isConnected = true;
                            ConnectedDeviceName = deviceInfo.Name;
                            NotifyStatus($"✓ Connected to {deviceInfo.Name}");
                            AutoConnected?.Invoke(this, deviceInfo);
                            return;
                        }
                    }
                }
                await Task.Delay(500 * (r + 1));
            }
            throw new Exception($"Hello Fairy service not found on {deviceInfo.Name}");
        }

        private void OnDisconnect(BluetoothLEDevice sender, object args)
        {
            if (sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
            {
                _isConnected = false;
                ConnectedDeviceName = null;
                NotifyStatus("Disconnected.");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_device != null)
            {
                _device.ConnectionStatusChanged -= OnDisconnect;
                _device.Dispose();
                _device = null;
            }
            _commandCharacteristic = null;
            _isConnected = false;
            ConnectedDeviceName = null;
            NotifyStatus("Disconnected.");
            await Task.CompletedTask;
        }

        private async Task Write(byte[] data)
        {
            if (!_isConnected || _commandCharacteristic == null)
                throw new InvalidOperationException("Not connected.");
            var w = new DataWriter();
            w.WriteBytes(data);
            await _commandCharacteristic.WriteValueAsync(w.DetachBuffer(), GattWriteOption.WriteWithResponse);
        }

        public async Task SetPowerAsync(bool on)
        {
            await Write(HelloFairyProtocol.BuildPowerCommand(on));
            NotifyStatus(on ? "Power ON" : "Power OFF");
        }

        public async Task SetHsvAsync(int h, int s, int v)
            => await Write(HelloFairyProtocol.BuildColorCommand(h, s, v));

        public async Task SetPresetAsync(byte id, int b)
            => await Write(HelloFairyProtocol.BuildPresetCommand(id, b));

        public Task TurnOffAsync() => SetPowerAsync(false);

        private void NotifyStatus(string m) => StatusChanged?.Invoke(this, m);
    }
}