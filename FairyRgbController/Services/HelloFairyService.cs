using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InTheHand.Bluetooth;
using InTheHand.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using System.Runtime.InteropServices.WindowsRuntime;
using FairyRgbController.Models;
using FairyRgbController.Services;

namespace FairyRgbController.Services
{
    public class HelloFairyService : IFairyLedService
    {
        private BleDevice _device;
        private GattCharacteristic _commandCharacteristic;
        private bool _isConnected;

        public Task<bool> IsConnectedAsync() => Task.FromResult(_isConnected);

        public async Task<IReadOnlyList<BleDeviceInfo>> ScanAsync(int timeoutMs = 10000)
        {
            var selector = BleDevice.GetDeviceSelectorFromUuid(HelloFairyProtocol.ServiceUuid);
            var devices = await DeviceInformation.FindAllAsync(selector);
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

            _device = await BleDevice.FromIdAsync(deviceInfo.Id);
            if (_device == null)
                throw new Exception("Failed to get BleDevice from Id.");

            // Get GATT services
            var gattServicesResult = await _device.GetGattServicesAsync();
            if (gattServicesResult.Error != BluetoothError.Success)
                throw new Exception($"Failed to get services: {gattServicesResult.Error}");

            var service = gattServicesResult.Services.FirstOrDefault(s => s.Uuid == HelloFairyProtocol.ServiceUuid);
            if (service == null)
                throw new Exception("Hello Fairy service not found.");

            var characteristicsResult = await service.GetCharacteristicsAsync();
            if (characteristicsResult.Error != BluetoothError.Success)
                throw new Exception($"Failed to get characteristics: {characteristicsResult.Error}");

            _commandCharacteristic = characteristicsResult.Characteristics.FirstOrDefault(c => c.Uuid == HelloFairyProtocol.CommandCharacteristicUuid);
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
        }

        private async Task WriteCommandAsync(byte[] packet)
        {
            if (!_isConnected || _commandCharacteristic == null)
                throw new InvalidOperationException("Not connected to device.");

            await _commandCharacteristic.WriteAsync(packet.AsBuffer(), GattWriteOption.WriteWithResponse);
        }

        public Task SetPowerAsync(bool on)
        {
            var packet = HelloFairyProtocol.BuildPowerCommand(on);
            return WriteCommandAsync(packet);
        }

        public Task SetHsvAsync(int hue, int saturation, int value)
        {
            var packet = HelloFairyProtocol.BuildColorCommand(hue, saturation, value);
            return WriteCommandAsync(packet);
        }

        public Task SetPresetAsync(byte presetId, int brightness)
        {
            var packet = HelloFairyProtocol.BuildPresetCommand(presetId, brightness);
            return WriteCommandAsync(packet);
        }

        public Task TurnOffAsync() => SetPowerAsync(false);
    }
}
