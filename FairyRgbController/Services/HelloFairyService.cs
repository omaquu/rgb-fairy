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
            NotifyStatus("Scanning...");

            try
            {
                // Fix: Scan BOTH paired AND unpaired BLE devices
                // Previously only scanned unpaired (GetDeviceSelectorFromPairingState(false))
                // If Windows has paired/remembered the fairy, it won't appear in unpaired scan
                var paired = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
                var unpaired = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);

                // Try 3 rounds of scanning with short delay between each
                // BLE devices advertise intermittently; multiple rounds catches them
                for (int round = 0; round < 3; round++)
                {
                    var combined = $"({paired}) OR ({unpaired})";
                    var devices = await DeviceInformation.FindAllAsync(combined)
                        .AsTask().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs / 3));

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
                        }
                    }

                    if (list.Count > 0) break; // Found devices, stop early
                    await Task.Delay(1000); // Wait for more devices to advertise
                }
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

            NotifyStatus($"Found {list.Count} device(s)."); 
            DevicesUpdated?.Invoke(this, list);
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
                throw new Exception($"Failed to get BluetoothLEDevice from Id: {deviceInfo.Id}");

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
                throw new Exception("Failed to get GATT services. Ensure device is powered on.");

            var service = gattResult.Services?.FirstOrDefault(s =>
                s.Uuid == HelloFairyProtocol.ServiceUuid);
            if (service == null)
            {
                var services = gattResult.Services?.Select(s => s.Uuid.ToString()) ?? Enumerable.Empty<string>();
                throw new Exception(
                    $"Hello Fairy service not found. Expected: {HelloFairyProtocol.ServiceUuid}\n" +
                    $"Available: {(string.IsNullOrEmpty(string.Join(",", services)) ? "none" : string.Join(", ", services))}");
            }

            var charResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (charResult.Status != GattCommunicationStatus.Success)
                throw new Exception($"Failed to get characteristics: {charResult.Status}");

            _commandCharacteristic = charResult.Characteristics?.FirstOrDefault(c =>
                c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
            if (_commandCharacteristic == null)
                throw new Exception("Command characteristic not found.");

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