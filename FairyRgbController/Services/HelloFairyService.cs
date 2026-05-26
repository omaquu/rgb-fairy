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

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 10000)
        {
            var selector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
            var devices = await DeviceInformation.FindAllAsync(selector)
                .AsTask().WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));

            var list = new List<BleDeviceInfo>();
            foreach (var deviceInfo in devices)
            {
                list.Add(new BleDeviceInfo
                {
                    Id = deviceInfo.Id,
                    Name = deviceInfo.Name
                });
            }
            return list;
        }

        public async Task ConnectAsync(BleDeviceInfo deviceInfo)
        {
            if (_isConnected)
                await DisconnectAsync();

            _device = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
            if (_device == null)
                throw new Exception("Failed to get BluetoothLEDevice from Id.");

            var gattServicesResult = await _device.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            if (gattServicesResult.Status != GattCommunicationStatus.Success)
                throw new Exception($"Failed to get services: {gattServicesResult.Status}");

            var service = gattServicesResult.Services.FirstOrDefault(s =>
                s.Uuid == HelloFairyProtocol.ServiceUuid);
            if (service == null)
                throw new Exception("Hello Fairy service not found.");

            var characteristicsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
            if (characteristicsResult.Status != GattCommunicationStatus.Success)
                throw new Exception($"Failed to get characteristics: {characteristicsResult.Status}");

            _commandCharacteristic = characteristicsResult.Characteristics.FirstOrDefault(c =>
                c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
            if (_commandCharacteristic == null)
                throw new Exception("Command characteristic not found.");

            _isConnected = true;
        }

        public async Task DisconnectAsync()
        {
            if (_device != null)
            {
                _device.Dispose();
                _device = null;
            }
            _commandCharacteristic = null;
            _isConnected = false;
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
    }
}