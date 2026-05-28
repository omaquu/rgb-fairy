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

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 30000)
        {
            var list = new List<BleDeviceInfo>();

            try
            {
                // Quick scan: check paired devices first, then few rounds of discovery
                var fairyDevices = new List<BleDeviceInfo>();

                // Round 1: Check already paired fairy devices (instant)
                NotifyStatus("Checking paired devices...");
                var pairedSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
                var pairedDevs = await DeviceInformation.FindAllAsync(pairedSelector).AsTask().WaitAsync(TimeSpan.FromMilliseconds(3000));

                foreach (var di in pairedDevs)
                {
                    if (string.IsNullOrWhiteSpace(di.Name)) continue;
                    if (di.Name.IndexOf("fairy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        di.Name.IndexOf("hello", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (!list.Any(d => d.Id == di.Id))
                        {
                            list.Add(new BleDeviceInfo
                            {
                                Id = di.Id,
                                Name = di.Name,
                                IsPaired = true,
                                IsConnectable = true
                            });
                        }
                    }
                }

                fairyDevices = list.Where(d => d.Name.IndexOf("fairy", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                DevicesUpdated?.Invoke(this, fairyDevices);

                // If we found paired fairy devices, we're done
                if (fairyDevices.Count > 0)
                {
                    NotifyStatus($"Found {fairyDevices.Count} paired fairy device(s)");
                    return list;
                }

                // Round 2-3: Quick discovery scan
                for (int round = 2; round <= 3; round++)
                {
                    NotifyStatus($"Scan {round}/3...");
                    var unpairedSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
                    var allSelector = $"({pairedSelector}) OR ({unpairedSelector})";

                    var devices = await DeviceInformation.FindAllAsync(allSelector)
                        .AsTask().WaitAsync(TimeSpan.FromMilliseconds(3000));

                    int added = 0;
                    foreach (var di in devices)
                    {
                        if (string.IsNullOrWhiteSpace(di.Name)) continue;
                        if (!list.Any(d => d.Id == di.Id))
                        {
                            list.Add(new BleDeviceInfo
                            {
                                Id = di.Id,
                                Name = di.Name,
                                IsPaired = IsPaired(di),
                                IsConnectable = di.IsEnabled
                            });
                            added++;
                        }
                    }

                    fairyDevices = list
                        .Where(d => d.Name.IndexOf("fairy", StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                    DevicesUpdated?.Invoke(this, fairyDevices);
                    NotifyStatus($"Round {round}: +{added} new, {fairyDevices.Count} fairy total");

                    if (fairyDevices.Count > 0) break;
                    if (round < 3) await Task.Delay(500);
                }

                NotifyStatus($"Done: {list.Count} device(s), {fairyDevices.Count} fairy");
                DevicesUpdated?.Invoke(this, fairyDevices);
            }
            catch (Exception ex)
            {
                NotifyStatus($"Error: {ex.Message}");
            }

            return list;
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
                            NotifyStatus($"✓ Connected to {deviceInfo.Name}");
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
            NotifyStatus("Disconnected.");
            await Task.CompletedTask;
        }

        private async Task Write(byte[] data)
        {
            if (!_isConnected || _commandCharacteristic == null) throw new InvalidOperationException();
            var w = new DataWriter();
            w.WriteBytes(data);
            await _commandCharacteristic.WriteValueAsync(w.DetachBuffer(), GattWriteOption.WriteWithResponse);
        }

        public async Task SetPowerAsync(bool on)
        {
            await Write(HelloFairyProtocol.BuildPowerCommand(on));
            NotifyStatus(on ? "ON" : "OFF");
        }

        public async Task SetHsvAsync(int h, int s, int v)
            => await Write(HelloFairyProtocol.BuildColorCommand(h, s, v));

        public async Task SetPresetAsync(byte id, int b)
            => await Write(HelloFairyProtocol.BuildPresetCommand(id, b));

        public async Task SendRawCommand(byte[] data)
        {
            AppLogger.WriteLine("DIY", $"TX RAW: {BitConverter.ToString(data)}");
            await Write(data);
        }

        public bool IsConnected => _isConnected;

        public Task TurnOffAsync() => SetPowerAsync(false);

        private void NotifyStatus(string m) => StatusChanged?.Invoke(this, m);
    }
}